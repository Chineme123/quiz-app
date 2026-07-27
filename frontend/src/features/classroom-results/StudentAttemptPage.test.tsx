import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { axe } from 'vitest-axe';
import { MemoryRouter, Routes, Route } from 'react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { StudentAttemptPage } from './StudentAttemptPage';
import * as api from './classroomResults.api';
import type { AttemptResult } from '@/features/results/results.schemas';

vi.mock('./classroomResults.api');

const QUIZ_ID = '44444444-0000-0000-0000-000000000004';
const STUDENT_ID = '66666666-0000-0000-0000-000000000001';

function renderPage(withState = true) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const entry = withState
    ? {
        pathname: `/quizzes/${QUIZ_ID}/results/students/${STUDENT_ID}`,
        state: { displayName: 'Alice', quizTitle: 'Cells and organelles' },
      }
    : `/quizzes/${QUIZ_ID}/results/students/${STUDENT_ID}`;
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[entry]}>
        <Routes>
          <Route path="quizzes/:quizId/results/students/:studentId" element={<StudentAttemptPage />} />
          <Route path="quizzes/:quizId/results" element={<h1>Quiz results</h1>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const attempt: AttemptResult = {
  attemptId: '77777777-0000-0000-0000-000000000001',
  quizId: QUIZ_ID,
  totalScore: 5,
  feedbackStatus: 'Ready',
  status: 'Reviewable',
  answers: [
    {
      questionId: '88888888-0000-0000-0000-000000000001',
      questionText: 'Which organelle makes energy?',
      providedAnswer: 'The nucleus',
      correctAnswer: 'The mitochondrion',
      isCorrect: false,
      pointsAwarded: 0,
      feedback: 'Close — energy is the mitochondrion’s job.',
      feedbackSource: 'Ai',
    },
    {
      questionId: '88888888-0000-0000-0000-000000000002',
      questionText: 'Do plant cells have a wall?',
      providedAnswer: 'Yes',
      correctAnswer: 'Yes',
      isCorrect: true,
      pointsAwarded: 5,
      feedback: 'That’s right.',
      feedbackSource: 'Deterministic',
    },
  ],
};

describe('StudentAttemptPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(api.getStudentAttempt).mockResolvedValue(attempt);
  });

  it('shows the attempt in the teacher voice, naming the student', async () => {
    renderPage();

    expect(await screen.findByRole('heading', { name: /Alice/ })).toBeInTheDocument();
    expect(screen.getByText(/Alice got/)).toBeInTheDocument();
    expect(screen.getByText(/1 of 2/)).toBeInTheDocument();
    // The teacher-voice label, not the student's "Your answer".
    expect(screen.getAllByText('Answer:').length).toBeGreaterThan(0);
    expect(screen.queryByText('Your answer:')).not.toBeInTheDocument();
  });

  it('shows the same breakdown the student sees, misses framed to review', async () => {
    renderPage();
    await screen.findByText('Which organelle makes energy?');

    expect(screen.getByText('To review')).toBeInTheDocument();
    expect(screen.getByText('Correct')).toBeInTheDocument();
    expect(screen.getByText('The mitochondrion')).toBeInTheDocument(); // correct answer for the miss
    expect(screen.getByText(/energy is the mitochondrion/)).toBeInTheDocument();
  });

  it('falls back to a generic label when opened without the student name', async () => {
    renderPage(false);
    expect(await screen.findByRole('heading', { name: /This student/ })).toBeInTheDocument();
  });

  it('reads as missing when the quiz is not yours or there is no submitted attempt', async () => {
    vi.mocked(api.getStudentAttempt).mockResolvedValue(null);
    renderPage();
    expect(await screen.findByRole('heading', { name: /couldn.t find that attempt/i })).toBeInTheDocument();
  });

  it('has no accessibility violations', async () => {
    const { container } = renderPage();
    await screen.findByText('Which organelle makes energy?');
    expect(await axe(container)).toHaveNoViolations();
  });
});
