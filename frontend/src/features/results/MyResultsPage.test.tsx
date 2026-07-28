import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { axe } from 'vitest-axe';
import { MemoryRouter, Routes, Route } from 'react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MyResultsPage } from './MyResultsPage';
import * as api from './myResults.api';

vi.mock('./myResults.api');

const CLASS_1 = '33333333-0000-0000-0000-000000000003';
const CLASS_2 = '33333333-0000-0000-0000-000000000007';
const QUIZ_1 = '44444444-0000-0000-0000-000000000004';
const QUIZ_2 = '44444444-0000-0000-0000-000000000005';
const ATTEMPT_1 = '55555555-0000-0000-0000-000000000001';
const ATTEMPT_2 = '55555555-0000-0000-0000-000000000002';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/results']}>
        <Routes>
          <Route path="results" element={<MyResultsPage />} />
          <Route path="results/:attemptId" element={<h1>Attempt detail</h1>} />
          <Route path="quizzes" element={<h1>Available quizzes</h1>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const myResults = {
  classrooms: [
    {
      classroomId: CLASS_1,
      classroomName: 'Networking',
      isArchived: false,
      standingPercent: 75,
      quizzes: [
        {
          quizId: QUIZ_1,
          title: 'Networking Basics',
          totalPoints: 4,
          score: 2,
          percent: 50,
          attemptId: ATTEMPT_1,
          submittedAt: '2026-07-27T10:00:00Z',
        },
        {
          quizId: QUIZ_2,
          title: 'Subnetting',
          totalPoints: 3,
          score: 3,
          percent: 100,
          attemptId: ATTEMPT_2,
          submittedAt: '2026-07-26T10:00:00Z',
        },
      ],
    },
    {
      classroomId: CLASS_2,
      classroomName: 'Study Group',
      isArchived: true,
      standingPercent: null,
      quizzes: [],
    },
  ],
};

describe('MyResultsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(api.getMyResults).mockResolvedValue(myResults);
  });

  it('groups finished quizzes by class, shows a standing, and links each row to its attempt', async () => {
    renderPage();

    expect(await screen.findByText('Networking')).toBeInTheDocument();
    // Each finished quiz links into the existing per attempt detail screen (AC-5).
    expect(screen.getByRole('link', { name: 'Networking Basics' })).toHaveAttribute(
      'href',
      `/results/${ATTEMPT_1}`,
    );
    expect(screen.getByRole('link', { name: 'Subnetting' })).toHaveAttribute(
      'href',
      `/results/${ATTEMPT_2}`,
    );
    // Each row reads as a percent and the points out of the total.
    expect(screen.getByText('50%')).toBeInTheDocument();
    expect(screen.getByText('2 / 4')).toBeInTheDocument();
    expect(screen.getByText('100%')).toBeInTheDocument();
    // The per class standing (the average of the two, distinct from either quiz) is shown.
    expect(screen.getByText(/standing at/i)).toBeInTheDocument();
    expect(screen.getByText('75%')).toBeInTheDocument();
  });

  it('shows an archived class the student is in, with a gentle empty note when nothing is finished', async () => {
    renderPage();

    expect(await screen.findByText('Study Group')).toBeInTheDocument();
    expect(screen.getByText(/archived/i)).toBeInTheDocument();
    // The group appears rather than silently disappearing, with a note instead of a standing (AC-2).
    expect(screen.getByText(/Nothing finished here yet/i)).toBeInTheDocument();
  });

  it('shows a warm empty state, pointing to available quizzes, when nothing is finished anywhere', async () => {
    vi.mocked(api.getMyResults).mockResolvedValue({ classrooms: [] });
    renderPage();

    expect(await screen.findByText(/haven.t finished a quiz yet/i)).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /available to take/i })).toHaveAttribute('href', '/quizzes');
  });

  it('shows a calm error state with a retry when the results fail to load', async () => {
    vi.mocked(api.getMyResults).mockRejectedValue(new Error('boom'));
    renderPage();

    expect(await screen.findByText(/couldn.t load your results/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /try again/i })).toBeInTheDocument();
  });

  it('has no accessibility violations', async () => {
    const { container } = renderPage();
    await screen.findByText('Networking');
    expect(await axe(container)).toHaveNoViolations();
  });
});
