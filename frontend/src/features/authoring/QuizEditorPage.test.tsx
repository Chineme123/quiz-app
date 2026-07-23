import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe } from 'vitest-axe';
import { MemoryRouter, Routes, Route } from 'react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ToastProvider } from '@/components/ui';
import { QuizEditorPage } from './QuizEditorPage';
import type { AuthoredQuiz } from './authoring.schemas';
import * as api from './authoring.api';

vi.mock('./authoring.api');

const CLASS_ID = '33333333-0000-0000-0000-000000000003';
const QUIZ_ID = '44444444-0000-0000-0000-000000000004';
const QUESTION_ID = '55555555-0000-0000-0000-000000000005';
const TEACHER_ID = '11111111-0000-0000-0000-000000000001';

function quiz(overrides: Partial<AuthoredQuiz> = {}): AuthoredQuiz {
  return {
    id: QUIZ_ID,
    title: 'Cells and organelles',
    durationMinutes: 10,
    classroomId: CLASS_ID,
    teacherId: TEACHER_ID,
    isPublished: false,
    availableFrom: null,
    availableTo: null,
    maxAttempts: 1,
    isLocked: false,
    questions: [
      {
        id: QUESTION_ID,
        questionType: 'MultipleChoice',
        prompt: 'What powers the cell?',
        points: 5,
        options: ['Nucleus', 'Mitochondria'],
        correctOptionIndex: 1,
        correctAnswerBool: null,
        correctAnswerText: null,
      },
    ],
    ...overrides,
  };
}

function renderEditor() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <MemoryRouter initialEntries={[`/quizzes/${QUIZ_ID}/edit`]}>
          <Routes>
            <Route path="quizzes/:quizId/edit" element={<QuizEditorPage />} />
            <Route path="classrooms/:classroomId/quizzes" element={<h1>Class quizzes</h1>} />
            <Route path="dashboard" element={<h1>Your classes</h1>} />
          </Routes>
        </MemoryRouter>
      </ToastProvider>
    </QueryClientProvider>,
  );
}

describe('QuizEditorPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(api.getQuiz).mockResolvedValue(quiz());
  });

  it('shows each question with its type, points, and correct answer', async () => {
    renderEditor();

    expect(await screen.findByText(/What powers the cell\?/)).toBeInTheDocument();
    // The teacher needs to see which answer is right to be able to edit it at all.
    expect(screen.getByText(/Multiple choice · 5 points · Answer: Mitochondria/)).toBeInTheDocument();
  });

  it('fixes the question set once a student has an attempt', async () => {
    vi.mocked(api.getQuiz).mockResolvedValue(quiz({ isLocked: true }));

    renderEditor();

    expect(await screen.findByText(/its questions are set now/i)).toBeInTheDocument();
    // No way in to changing questions, rather than offering it and being refused.
    expect(screen.queryByRole('button', { name: 'Add a question' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Edit' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Remove' })).not.toBeInTheDocument();
    // The settings still change while locked.
    expect(screen.getByRole('button', { name: /publish/i })).toBeInTheDocument();
  });

  it('adds a question', async () => {
    vi.mocked(api.addQuestion).mockResolvedValue(quiz());
    renderEditor();
    await screen.findByText(/What powers the cell\?/);

    await userEvent.click(screen.getByRole('button', { name: 'Add a question' }));
    const dialog = within(screen.getByRole('dialog'));
    await userEvent.selectOptions(dialog.getByLabelText(/type/i), 'TrueFalse');
    // By role, not label: the dialog's own accessible name is "Add a question", which a
    // /question/i label query also matches.
    await userEvent.type(dialog.getByRole('textbox', { name: /question/i }), 'The sky is blue.');
    await userEvent.click(dialog.getByRole('button', { name: 'Add question' }));

    expect(api.addQuestion).toHaveBeenCalledWith(QUIZ_ID, {
      questionType: 'TrueFalse',
      prompt: 'The sky is blue.',
      points: 1,
      correctAnswerBool: true,
    });
  });

  it('confirms before publishing, then publishes with the chosen settings', async () => {
    vi.mocked(api.publishQuiz).mockResolvedValue(quiz({ isPublished: true }));
    renderEditor();
    await screen.findByText(/What powers the cell\?/);

    await userEvent.click(screen.getByRole('button', { name: 'Publish' }));

    // Publishing changes what students can reach, so it asks first (ui-rules §1).
    const dialog = within(await screen.findByRole('dialog'));
    expect(dialog.getByText(/able to start it/i)).toBeInTheDocument();
    await userEvent.click(dialog.getByRole('button', { name: 'Publish' }));

    expect(api.publishQuiz).toHaveBeenCalledWith(QUIZ_ID, {
      availableFrom: null,
      availableTo: null,
      maxAttempts: 1,
    });
  });

  it('will not publish a quiz with no questions', async () => {
    vi.mocked(api.getQuiz).mockResolvedValue(quiz({ questions: [] }));

    renderEditor();

    expect(await screen.findByText(/add at least one question before publishing/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Publish' })).toBeDisabled();
  });

  it('reads as missing when the quiz is not yours', async () => {
    vi.mocked(api.getQuiz).mockResolvedValue(null);

    renderEditor();

    expect(
      await screen.findByRole('heading', { name: /couldn.t find that quiz/i }),
    ).toBeInTheDocument();
  });

  it('has no accessibility violations', async () => {
    const { container } = renderEditor();
    await screen.findByText(/What powers the cell\?/);

    expect(await axe(container)).toHaveNoViolations();
  });
});
