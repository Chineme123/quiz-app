import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { axe } from 'vitest-axe';
import { MemoryRouter, Routes, Route } from 'react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { StudentRollupPage } from './StudentRollupPage';
import * as api from './classroomResults.api';

vi.mock('./classroomResults.api');

const CLASS_ID = '33333333-0000-0000-0000-000000000003';
const SMALL = '44444444-0000-0000-0000-000000000004';
const BIG = '44444444-0000-0000-0000-000000000005';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/classrooms/${CLASS_ID}/results/students`]}>
        <Routes>
          <Route path="classrooms/:classroomId/results/students" element={<StudentRollupPage />} />
          <Route path="quizzes/:quizId/results/students/:studentId" element={<h1>The attempt</h1>} />
          <Route path="classrooms/:classroomId/results" element={<h1>Class results</h1>} />
          <Route path="dashboard" element={<h1>Your classes</h1>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const rollup = {
  classroomId: CLASS_ID,
  classroomName: 'Biology 101',
  quizzes: [
    { quizId: SMALL, title: 'Small quiz', totalPoints: 10 },
    { quizId: BIG, title: 'Big quiz', totalPoints: 100 },
  ],
  students: [
    {
      studentId: '66666666-0000-0000-0000-000000000001',
      displayName: 'Alice',
      scores: [
        { quizId: SMALL, status: 'Completed', score: 5, percent: 50, attemptId: '77777777-0000-0000-0000-000000000001' },
        { quizId: BIG, status: 'Completed', score: 80, percent: 80, attemptId: '77777777-0000-0000-0000-000000000002' },
      ],
      overallStandingPercent: 65,
    },
    {
      studentId: '66666666-0000-0000-0000-000000000002',
      displayName: 'Bob',
      scores: [
        { quizId: SMALL, status: 'NotTaken', score: null, percent: null, attemptId: null },
        { quizId: BIG, status: 'InProgress', score: null, percent: null, attemptId: null },
      ],
      overallStandingPercent: null,
    },
  ],
  total: 2,
  page: 1,
  pageSize: 20,
} as const;

describe('StudentRollupPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(api.getStudentRollup).mockResolvedValue(rollup);
  });

  it('shows each student with a normalized overall standing and per-quiz scores', async () => {
    renderPage();

    expect(await screen.findByText('Alice')).toBeInTheDocument();
    // The overall standing is the average of the quiz percentages (50 and 80), not of raw points.
    expect(screen.getByText('65%')).toBeInTheDocument();
    expect(screen.getAllByText('Small quiz').length).toBeGreaterThan(0);
    expect(screen.getByRole('link', { name: /5 \/ 10/ })).toBeInTheDocument();
  });

  it('shows a student who has not started as such, not as a zero', async () => {
    renderPage();
    await screen.findByText('Alice');

    expect(screen.getByText('Not started yet')).toBeInTheDocument();
    expect(screen.getByText('In progress')).toBeInTheDocument();
    expect(screen.getByText('Not taken')).toBeInTheDocument();
  });

  it('reads as missing when the class is not yours', async () => {
    vi.mocked(api.getStudentRollup).mockResolvedValue(null);
    renderPage();
    expect(await screen.findByRole('heading', { name: /couldn.t find that class/i })).toBeInTheDocument();
  });

  it('has no accessibility violations', async () => {
    const { container } = renderPage();
    await screen.findByText('Alice');
    expect(await axe(container)).toHaveNoViolations();
  });
});
