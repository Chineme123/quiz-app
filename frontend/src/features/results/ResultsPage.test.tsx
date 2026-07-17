import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { axe } from 'vitest-axe';
import { MemoryRouter, Routes, Route } from 'react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ResultsPage } from './ResultsPage';
import * as api from './results.api';
import type { AttemptResult } from './results.schemas';

vi.mock('./results.api');

function renderResults(id = 'abc') {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/results/${id}`]}>
        <Routes>
          <Route path="results/:attemptId" element={<ResultsPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const readyResult: AttemptResult = {
  attemptId: 'abc',
  quizId: 'q1',
  totalScore: 1,
  feedbackStatus: 'Ready',
  status: 'Reviewable',
  answers: [
    {
      questionId: 'x1',
      questionText: 'Which layer routes packets between networks?',
      providedAnswer: 'The transport layer',
      correctAnswer: 'The network layer',
      isCorrect: false,
      pointsAwarded: 0,
      feedback: 'So close — routing between networks happens at the network layer.',
      feedbackSource: 'Ai',
    },
    {
      questionId: 'x2',
      questionText: 'What does DNS translate a domain name into?',
      providedAnswer: 'IP address',
      correctAnswer: 'IP address',
      isCorrect: true,
      pointsAwarded: 1,
      feedback: "Nice — that's right.",
      feedbackSource: 'Deterministic',
    },
  ],
};

describe('ResultsPage (AC-8, AC-12)', () => {
  beforeEach(() => {
    vi.resetAllMocks();
  });

  it('shows the score and the per question breakdown when feedback is ready', async () => {
    vi.mocked(api.getAttemptResult).mockResolvedValue(readyResult);
    renderResults();

    expect(await screen.findByText(/1 of 2/)).toBeInTheDocument();
    expect(screen.getByText('Which layer routes packets between networks?')).toBeInTheDocument();
    // The wrong answer is flagged to review and shows the correct answer.
    expect(screen.getByText('To review')).toBeInTheDocument();
    expect(screen.getByText('The network layer')).toBeInTheDocument();
    expect(screen.getByText('Correct')).toBeInTheDocument();
    // Both AI and deterministic feedback render, the same way.
    expect(screen.getByText(/routing between networks happens at the network layer/)).toBeInTheDocument();
    expect(screen.getByText(/Nice — that's right/)).toBeInTheDocument();
  });

  it('shows a generating state while feedback is still pending (AC-12)', async () => {
    vi.mocked(api.getAttemptResult).mockResolvedValue({
      ...readyResult,
      feedbackStatus: 'Pending',
      answers: readyResult.answers.map((a) => ({ ...a, feedback: null, feedbackSource: null })),
    });
    renderResults();

    // Score is shown immediately, feedback is still being written.
    expect(await screen.findByText(/1 of 2/)).toBeInTheDocument();
    expect(screen.getAllByText(/Quiztin is writing your feedback/).length).toBeGreaterThan(0);
  });

  it('renders a skipped question honestly, not as a blank answer (spec 0006)', async () => {
    // A skipped question now arrives as a graded row with a blank answer (the backend completes
    // the record at grading). The screen must read "Not answered", never an empty value, and the
    // count must include it so the headline cannot overstate the score.
    vi.mocked(api.getAttemptResult).mockResolvedValue({
      ...readyResult,
      totalScore: 1,
      answers: [
        ...readyResult.answers,
        {
          questionId: 'x3',
          questionText: 'Which protocol assigns IP addresses automatically?',
          providedAnswer: '',
          correctAnswer: 'DHCP',
          isCorrect: false,
          pointsAwarded: 0,
          feedback: 'Not quite — worth another look at this one.',
          feedbackSource: 'Deterministic',
        },
      ],
    });
    renderResults();

    // Three questions now, one correct: the count is honest, not "every one right".
    expect(await screen.findByText(/1 of 3/)).toBeInTheDocument();
    expect(screen.getByText('Not answered')).toBeInTheDocument();
    expect(screen.getByText('DHCP')).toBeInTheDocument();
  });

  it('shows a not found state for an unknown attempt or one that is not the caller’s (AC-9)', async () => {
    vi.mocked(api.getAttemptResult).mockResolvedValue(null);
    renderResults();
    expect(await screen.findByText(/find that result/)).toBeInTheDocument();
  });

  it('has no accessibility violations', async () => {
    vi.mocked(api.getAttemptResult).mockResolvedValue(readyResult);
    const { container } = renderResults();
    await screen.findByText(/1 of 2/);
    expect(await axe(container)).toHaveNoViolations();
  });
});
