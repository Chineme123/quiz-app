import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { axe } from 'vitest-axe';
import { MemoryRouter, Routes, Route } from 'react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { QuizResultsPage } from './QuizResultsPage';
import * as api from './classroomResults.api';
import type { QuizResults } from './classroomResults.schemas';

vi.mock('./classroomResults.api');

const CLASS_ID = '33333333-0000-0000-0000-000000000003';
const QUIZ_ID = '44444444-0000-0000-0000-000000000004';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/quizzes/${QUIZ_ID}/results`]}>
        <Routes>
          <Route path="quizzes/:quizId/results" element={<QuizResultsPage />} />
          <Route path="quizzes/:quizId/results/students/:studentId" element={<h1>The attempt</h1>} />
          <Route path="classrooms/:classroomId/results" element={<h1>Class results</h1>} />
          <Route path="dashboard" element={<h1>Your classes</h1>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const results: QuizResults = {
  quizId: QUIZ_ID,
  classroomId: CLASS_ID,
  title: 'Cells and organelles',
  totalPoints: 10,
  studentCount: 4,
  completionCount: 2,
  averageScore: 7.5,
  averagePercent: 75,
  questions: [
    { questionId: '55555555-0000-0000-0000-000000000001', prompt: 'What is a cell?', points: 5, correctCount: 2, answeredCount: 2, fractionCorrect: 100 },
    { questionId: '55555555-0000-0000-0000-000000000002', prompt: 'The hard one', points: 5, correctCount: 0, answeredCount: 2, fractionCorrect: 0 },
  ],
  students: [
    { studentId: '66666666-0000-0000-0000-000000000001', displayName: 'Alice', status: 'Completed', score: 10, percent: 100, attemptId: '77777777-0000-0000-0000-000000000001' },
    { studentId: '66666666-0000-0000-0000-000000000002', displayName: 'Bob', status: 'InProgress', score: null, percent: null, attemptId: null },
    { studentId: '66666666-0000-0000-0000-000000000003', displayName: 'carol@school.edu', status: 'NotTaken', score: null, percent: null, attemptId: null },
  ],
  total: 4,
  page: 1,
  pageSize: 20,
};

describe('QuizResultsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(api.getQuizResults).mockResolvedValue(results);
  });

  it('shows per-question difficulty and flags the ones worth reviewing', async () => {
    renderPage();

    expect(await screen.findByText('What is a cell?')).toBeInTheDocument();
    expect(screen.getByText('The hard one')).toBeInTheDocument();
    // The question the class mostly missed is flagged, calmly.
    expect(screen.getByText(/Worth reviewing together/)).toBeInTheDocument();
    expect(screen.getByText(/class average 75%/)).toBeInTheDocument();
  });

  it('lists each student by name with their status and a link into a finished attempt', async () => {
    renderPage();
    await screen.findByText('What is a cell?');

    // A finished student shows their score as a link into the attempt.
    const attemptLink = screen.getByRole('link', { name: /10 \/ 10/ });
    expect(attemptLink).toBeInTheDocument();
    expect(screen.getByText('Alice')).toBeInTheDocument();
    // In progress and not taken read as words, not a zero (AC-8), and names are never bare ids.
    expect(screen.getByText('In progress')).toBeInTheDocument();
    expect(screen.getByText('Not taken')).toBeInTheDocument();
    expect(screen.getByText('carol@school.edu')).toBeInTheDocument();
  });

  it('reads as missing when the quiz is not yours', async () => {
    vi.mocked(api.getQuizResults).mockResolvedValue(null);
    renderPage();
    expect(await screen.findByRole('heading', { name: /couldn.t find that quiz/i })).toBeInTheDocument();
  });

  it('has no accessibility violations', async () => {
    const { container } = renderPage();
    await screen.findByText('What is a cell?');
    expect(await axe(container)).toHaveNoViolations();
  });
});
