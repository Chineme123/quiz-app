import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe } from 'vitest-axe';
import { MemoryRouter, Routes, Route } from 'react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { QuizListPage } from './QuizListPage';
import * as api from './take.api';
import type { AvailableQuizzes } from './take.schemas';

vi.mock('./take.api');

function renderList() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/quizzes']}>
        <Routes>
          <Route path="quizzes" element={<QuizListPage />} />
          <Route path="attempts/:attemptId/take" element={<h1>Taking the quiz</h1>} />
          <Route path="results/:attemptId" element={<h1>Your result</h1>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const threeStates: AvailableQuizzes = {
  total: 3,
  page: 1,
  pageSize: 20,
  items: [
    { quizId: 'q1', title: 'Not started yet', durationMinutes: 10, questionCount: 3, state: 'NotStarted', attemptId: null },
    { quizId: 'q2', title: 'Half done', durationMinutes: 20, questionCount: 5, state: 'InProgress', attemptId: 'a2' },
    { quizId: 'q3', title: 'All finished', durationMinutes: 15, questionCount: 4, state: 'Graded', attemptId: 'a3' },
  ],
};

beforeEach(() => vi.clearAllMocks());

describe('QuizListPage', () => {
  it('offers the one action that fits each quiz', async () => {
    vi.mocked(api.getAvailableQuizzes).mockResolvedValue(threeStates);
    renderList();

    expect(await screen.findByText('Not started yet')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Start' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Resume' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'View result' })).toBeInTheDocument();
  });

  it('resumes an open attempt without starting a second one', async () => {
    vi.mocked(api.getAvailableQuizzes).mockResolvedValue(threeStates);
    const user = userEvent.setup();
    renderList();

    await user.click(await screen.findByRole('button', { name: 'Resume' }));

    // Straight to the attempt the list handed over: Resume never calls start, which is what
    // stops it burning a second attempt on a one-shot quiz (AC-2).
    expect(await screen.findByText('Taking the quiz')).toBeInTheDocument();
    expect(api.startQuiz).not.toHaveBeenCalled();
  });

  it('starts a quiz and goes to the take screen', async () => {
    vi.mocked(api.getAvailableQuizzes).mockResolvedValue(threeStates);
    vi.mocked(api.startQuiz).mockResolvedValue('new-attempt');
    const user = userEvent.setup();
    renderList();

    await user.click(await screen.findByRole('button', { name: 'Start' }));

    expect(await screen.findByText('Taking the quiz')).toBeInTheDocument();
    expect(api.startQuiz).toHaveBeenCalledWith('q1');
  });

  it('says so kindly when there is nothing to take', async () => {
    vi.mocked(api.getAvailableQuizzes).mockResolvedValue({ items: [], total: 0, page: 1, pageSize: 20 });
    renderList();

    expect(await screen.findByText(/No quizzes yet/)).toBeInTheDocument();
  });

  it('has no accessibility violations', async () => {
    vi.mocked(api.getAvailableQuizzes).mockResolvedValue(threeStates);
    const { container } = renderList();
    await screen.findByText('Not started yet');

    expect(await axe(container)).toHaveNoViolations();
  });
});
