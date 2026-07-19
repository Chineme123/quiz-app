import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe } from 'vitest-axe';
import { MemoryRouter, Routes, Route } from 'react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { TakeQuizPage } from './TakeQuizPage';
import * as api from './take.api';
import type { AttemptQuestions } from './take.schemas';

vi.mock('./take.api');

function renderTake(id = 'a1') {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/attempts/${id}/take`]}>
        <Routes>
          <Route path="attempts/:attemptId/take" element={<TakeQuizPage />} />
          <Route path="results/:attemptId" element={<h1>Your result</h1>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

/** A running attempt with ten minutes left and one answer already saved. */
function runningAttempt(overrides: Partial<AttemptQuestions> = {}): AttemptQuestions {
  const now = new Date('2026-07-16T12:00:00Z');
  return {
    attemptId: 'a1',
    quizTitle: 'Networking basics',
    status: 'InProgress',
    serverNow: now.toISOString(),
    expiresAt: new Date(now.getTime() + 10 * 60_000).toISOString(),
    draftAnswers: { q1: '1' },
    questions: [
      {
        id: 'q1',
        questionType: 'MultipleChoiceQuestion',
        prompt: 'Which layer routes packets between networks?',
        points: 10,
        options: ['Transport', 'Network', 'Physical'],
      },
      {
        id: 'q2',
        questionType: 'ShortAnswerQuestion',
        prompt: 'Name the protocol that resolves names to addresses.',
        points: 5,
        options: null,
      },
    ],
    ...overrides,
  };
}

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(api.saveDraftAnswers).mockResolvedValue(undefined);
});

describe('TakeQuizPage', () => {
  it('shows one question at a time with the countdown and the navigator', async () => {
    vi.mocked(api.getAttemptQuestions).mockResolvedValue(runningAttempt());
    renderTake();

    expect(await screen.findByText('Networking basics')).toBeInTheDocument();
    // One question in focus, not the whole form.
    expect(screen.getByText('Which layer routes packets between networks?')).toBeInTheDocument();
    expect(screen.queryByText('Name the protocol that resolves names to addresses.')).not.toBeInTheDocument();
    expect(screen.getByText(/Question 1 of 2/)).toBeInTheDocument();
    // The navigator says the state in words, not by colour alone. Await the "answered"
    // label: it settles a render after the title, once the saved draft answers hydrate the
    // form state, so a synchronous query here races that update (flaky in CI). Once Q1 reads
    // "answered" the navigator is fully rendered, so Q2 can be queried synchronously.
    expect(await screen.findByRole('button', { name: 'Question 1, answered' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Question 2, not answered yet' })).toBeInTheDocument();
  });

  it('restores the answers already saved, so a refresh costs nothing', async () => {
    vi.mocked(api.getAttemptQuestions).mockResolvedValue(runningAttempt());
    renderTake();

    // q1's saved answer was option index 1 -> "Network" is already selected on load (AC-9).
    const network = await screen.findByRole('radio', { name: 'Network' });
    expect(network).toBeChecked();
  });

  it('saves the whole answer set when an answer changes', async () => {
    vi.mocked(api.getAttemptQuestions).mockResolvedValue(runningAttempt());
    const user = userEvent.setup();
    renderTake();

    await user.click(await screen.findByRole('radio', { name: 'Physical' }));

    // Debounced, and it sends every answer it holds, not just the one that changed, so two
    // saves in flight can never interleave and drop one (AC-6).
    await waitFor(() => expect(api.saveDraftAnswers).toHaveBeenCalled());
    const [, sent] = vi.mocked(api.saveDraftAnswers).mock.calls.at(-1)!;
    expect(sent).toEqual({ q1: '2' });
    expect(await screen.findByText('Saved')).toBeInTheDocument();
  });

  it('warns about unanswered questions but still lets you submit', async () => {
    vi.mocked(api.getAttemptQuestions).mockResolvedValue(runningAttempt());
    vi.mocked(api.submitAttempt).mockResolvedValue({
      attemptId: 'a1',
      quizId: 'q',
      totalScore: 10,
      feedbackStatus: 'Pending',
      status: 'Graded',
      answers: [],
    });
    const user = userEvent.setup();
    renderTake();

    await user.click(await screen.findByRole('button', { name: 'Submit quiz' }));

    // q2 is blank: warn, never block — a student who doesn't know an answer must still finish.
    expect(await screen.findByRole('dialog')).toBeInTheDocument();
    expect(screen.getByText(/left 1 question unanswered/i)).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Keep working' }));
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('submits with only an idempotency key and goes to the result', async () => {
    vi.mocked(api.getAttemptQuestions).mockResolvedValue(runningAttempt());
    vi.mocked(api.submitAttempt).mockResolvedValue({
      attemptId: 'a1',
      quizId: 'q',
      totalScore: 10,
      feedbackStatus: 'Pending',
      status: 'Graded',
      answers: [],
    });
    const user = userEvent.setup();
    renderTake();

    await user.click(await screen.findByRole('button', { name: 'Submit quiz' }));
    await user.click(screen.getByRole('button', { name: 'Submit' }));

    await waitFor(() => expect(api.submitAttempt).toHaveBeenCalled());
    // The answers are already on the server; submit carries the attempt and a command id only.
    const [attemptId, commandId] = vi.mocked(api.submitAttempt).mock.calls[0]!;
    expect(attemptId).toBe('a1');
    expect(commandId).toEqual(expect.any(String));
    expect(await screen.findByText('Your result')).toBeInTheDocument();
  });

  it('shows a not-found state for an attempt that is not yours', async () => {
    // The API returns null for both "missing" and "someone else's", and so does the screen:
    // it never reveals that another student's attempt exists (AC-5).
    vi.mocked(api.getAttemptQuestions).mockResolvedValue(null);
    renderTake();

    expect(await screen.findByText("We couldn't find that quiz")).toBeInTheDocument();
  });

  it('has no accessibility violations', async () => {
    vi.mocked(api.getAttemptQuestions).mockResolvedValue(runningAttempt());
    const { container } = renderTake();
    await screen.findByText('Networking basics');

    expect(await axe(container)).toHaveNoViolations();
  });
});
