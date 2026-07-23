import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe } from 'vitest-axe';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ToastProvider } from '@/components/ui';
import { GenerationSection } from './GenerationSection';
import type { AuthoredQuiz, DraftCandidate, GeneratedDraft } from './authoring.schemas';
import * as api from './authoring.api';

vi.mock('./authoring.api');

const CLASS_ID = '33333333-0000-0000-0000-000000000003';
const QUIZ_ID = '44444444-0000-0000-0000-000000000004';
const FIRST_ID = '66666666-0000-0000-0000-000000000006';
const SECOND_ID = '77777777-0000-0000-0000-000000000007';

const FIRST: DraftCandidate = {
  id: FIRST_ID,
  questionType: 'MultipleChoice',
  prompt: 'Where does photosynthesis happen?',
  points: 5,
  options: ['Nucleus', 'Chloroplast'],
  correctOptionIndex: 1,
  correctAnswerBool: null,
  correctAnswerText: null,
};

const SECOND: DraftCandidate = {
  id: SECOND_ID,
  questionType: 'TrueFalse',
  prompt: 'Plants release oxygen.',
  points: 1,
  options: null,
  correctOptionIndex: null,
  correctAnswerBool: true,
  correctAnswerText: null,
};

function batch(candidates: DraftCandidate[]): GeneratedDraft {
  return { quizId: QUIZ_ID, createdAt: '2026-07-23T10:00:00Z', candidates };
}

/** Accept answers with the whole quiz; the section only needs it to be a well formed one. */
function quizAfterAccept(): AuthoredQuiz {
  return {
    id: QUIZ_ID,
    title: 'Photosynthesis',
    durationMinutes: 10,
    classroomId: CLASS_ID,
    teacherId: '11111111-0000-0000-0000-000000000001',
    isPublished: false,
    availableFrom: null,
    availableTo: null,
    maxAttempts: 1,
    isLocked: false,
    questions: [],
  };
}

function renderSection() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <GenerationSection quizId={QUIZ_ID} classroomId={CLASS_ID} />
      </ToastProvider>
    </QueryClientProvider>,
  );
}

describe('GenerationSection', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(api.getDrafts).mockResolvedValue(null);
  });

  it('asks what the questions should cover before it will draft anything', async () => {
    renderSection();

    expect(await screen.findByRole('button', { name: /draft questions/i })).toBeDisabled();
  });

  it('sends what the teacher asked for', async () => {
    const user = userEvent.setup();
    vi.mocked(api.generateQuestions).mockResolvedValue(batch([FIRST]));

    renderSection();

    await user.type(await screen.findByRole('textbox', { name: /cover/i }), 'Photosynthesis');
    await user.click(screen.getByRole('button', { name: /draft questions/i }));

    expect(api.generateQuestions).toHaveBeenCalledWith(QUIZ_ID, {
      topic: 'Photosynthesis',
      difficulty: 'medium',
      count: 5,
      sourceText: '',
      file: null,
    });
  });

  it('shows a waiting batch with each answer, and adds nothing on its own', async () => {
    vi.mocked(api.getDrafts).mockResolvedValue(batch([FIRST, SECOND]));

    renderSection();

    expect(await screen.findByText(/Where does photosynthesis happen\?/)).toBeInTheDocument();
    expect(screen.getByText(/Multiple choice · 5 points ·\s*Answer: Chloroplast/)).toBeInTheDocument();
    expect(screen.getByText(/True or false · 1 point ·\s*Answer: True/)).toBeInTheDocument();
    // Reviewing is the point: nothing reaches the quiz until the teacher says so.
    expect(api.acceptDrafts).not.toHaveBeenCalled();
    expect(screen.getByRole('button', { name: 'Add 2 to the quiz' })).toBeInTheDocument();
  });

  it('adds only the ones the teacher kept', async () => {
    const user = userEvent.setup();
    vi.mocked(api.getDrafts).mockResolvedValue(batch([FIRST, SECOND]));
    vi.mocked(api.acceptDrafts).mockResolvedValue(quizAfterAccept());

    renderSection();

    // Skip the second one, so only the first should be promoted.
    const [, second] = await screen.findAllByRole('button', { name: 'Keeping' });
    if (second === undefined) throw new Error('expected both candidates to be listed');
    await user.click(second);

    await user.click(screen.getByRole('button', { name: 'Add 1 to the quiz' }));

    expect(api.acceptDrafts).toHaveBeenCalledWith(QUIZ_ID, [FIRST_ID]);
  });

  it('confirms before clearing a batch, because it cannot be got back', async () => {
    const user = userEvent.setup();
    vi.mocked(api.getDrafts).mockResolvedValue(batch([FIRST]));
    vi.mocked(api.discardDrafts).mockResolvedValue(undefined);

    renderSection();

    await user.click(await screen.findByRole('button', { name: /clear these/i }));
    expect(api.discardDrafts).not.toHaveBeenCalled();

    await user.click(screen.getByRole('button', { name: 'Clear them' }));
    expect(api.discardDrafts).toHaveBeenCalledWith(QUIZ_ID);
  });

  it('says so plainly when nothing usable came back', async () => {
    const user = userEvent.setup();
    vi.mocked(api.generateQuestions).mockResolvedValue(batch([]));

    renderSection();

    await user.type(await screen.findByRole('textbox', { name: /cover/i }), 'Photosynthesis');
    await user.click(screen.getByRole('button', { name: /draft questions/i }));

    expect(await screen.findByText(/couldn't draft anything usable/i)).toBeInTheDocument();
  });

  it('refuses a file over the cap here rather than letting the server reject it', async () => {
    const user = userEvent.setup();
    renderSection();

    const tooBig = new File([new Uint8Array(6 * 1024 * 1024)], 'lesson.pdf', {
      type: 'application/pdf',
    });
    await user.upload(await screen.findByLabelText(/attach a pdf or word file/i), tooBig);

    expect(await screen.findByRole('alert')).toHaveTextContent(/over 5 MB/i);
    expect(api.generateQuestions).not.toHaveBeenCalled();
  });

  it('has no accessibility violations while reviewing a batch', async () => {
    vi.mocked(api.getDrafts).mockResolvedValue(batch([FIRST, SECOND]));

    const { container } = renderSection();
    await screen.findByText(/Where does photosynthesis happen\?/);

    expect(await axe(container)).toHaveNoViolations();
  });
});
