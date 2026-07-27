import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { axe } from 'vitest-axe';
import { MemoryRouter, Routes, Route } from 'react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ClassroomResultsPage } from './ClassroomResultsPage';
import * as api from './classroomResults.api';

vi.mock('./classroomResults.api');

const CLASS_ID = '33333333-0000-0000-0000-000000000003';
const QUIZ_ID = '44444444-0000-0000-0000-000000000004';
const QUIZ_ID_2 = '44444444-0000-0000-0000-000000000005';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/classrooms/${CLASS_ID}/results`]}>
        <Routes>
          <Route path="classrooms/:classroomId/results" element={<ClassroomResultsPage />} />
          <Route path="classrooms/:classroomId/results/students" element={<h1>Standings</h1>} />
          <Route path="quizzes/:quizId/results" element={<h1>Quiz results</h1>} />
          <Route path="classrooms/:classroomId" element={<h1>The class</h1>} />
          <Route path="dashboard" element={<h1>Your classes</h1>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const summary = {
  classroomId: CLASS_ID,
  classroomName: 'Biology 101',
  isArchived: false,
  studentCount: 3,
  quizzes: [
    {
      quizId: QUIZ_ID,
      title: 'Cells and organelles',
      isPublished: true,
      totalPoints: 10,
      completionCount: 2,
      averageScore: 7.5,
      averagePercent: 75,
    },
    {
      quizId: QUIZ_ID_2,
      title: 'Nobody yet',
      isPublished: true,
      totalPoints: 10,
      completionCount: 0,
      averageScore: null,
      averagePercent: null,
    },
  ],
};

describe('ClassroomResultsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(api.getClassroomResults).mockResolvedValue(summary);
  });

  it('shows each quiz with its completion count and class average', async () => {
    renderPage();

    expect(await screen.findByText('Biology 101')).toBeInTheDocument();
    expect(screen.getByText('Cells and organelles')).toBeInTheDocument();
    expect(screen.getByText('75%')).toBeInTheDocument();
    expect(screen.getByText(/2 of 3 finished/)).toBeInTheDocument();
    // A quiz nobody has finished shows no average, not a zero.
    expect(screen.getByText(/No one has finished yet/)).toBeInTheDocument();
  });

  it('offers a way into the per-student standings', async () => {
    renderPage();
    expect(await screen.findByRole('link', { name: /each student.s standing/i })).toBeInTheDocument();
  });

  it('shows a gentle empty state when there are no quizzes to report', async () => {
    vi.mocked(api.getClassroomResults).mockResolvedValue({ ...summary, quizzes: [] });
    renderPage();
    expect(await screen.findByText(/No results to show yet/)).toBeInTheDocument();
  });

  it('reads as missing when the class is not yours', async () => {
    vi.mocked(api.getClassroomResults).mockResolvedValue(null);
    renderPage();
    expect(await screen.findByRole('heading', { name: /couldn.t find that class/i })).toBeInTheDocument();
  });

  it('has no accessibility violations', async () => {
    const { container } = renderPage();
    await screen.findByText('Biology 101');
    expect(await axe(container)).toHaveNoViolations();
  });
});
