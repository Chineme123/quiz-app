import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe } from 'vitest-axe';
import { MemoryRouter, Routes, Route } from 'react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AuthContext } from '@/lib/auth/useAuth';
import { ToastProvider } from '@/components/ui';
import { makeAuthValue } from '@/test/authValue';
import type { Role } from '@/lib/auth/session';
import { DashboardPage } from './DashboardPage';
import * as api from './classrooms.api';

vi.mock('./classrooms.api');

const CLASS_ID = '33333333-0000-0000-0000-000000000003';

function renderDashboard(role: Role) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <AuthContext.Provider value={makeAuthValue('authenticated', role)}>
        <ToastProvider>
          <MemoryRouter initialEntries={['/dashboard']}>
            <Routes>
              <Route path="dashboard" element={<DashboardPage />} />
              <Route path="quizzes" element={<h1>Your quizzes</h1>} />
            </Routes>
          </MemoryRouter>
        </ToastProvider>
      </AuthContext.Provider>
    </QueryClientProvider>,
  );
}

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(api.getOwnedClassrooms).mockResolvedValue([]);
  vi.mocked(api.getEnrolledClassrooms).mockResolvedValue([]);
});

describe('DashboardPage', () => {
  it('gives a teacher the classes they run, not the join box', async () => {
    vi.mocked(api.getOwnedClassrooms).mockResolvedValue([
      {
        id: CLASS_ID,
        name: 'Biology 101',
        joinCode: 'ABC234',
        studentCount: 2,
        quizCount: 1,
        createdAt: '2026-07-19T00:00:00Z',
        archivedAt: null,
      },
    ]);

    renderDashboard('Teacher');

    expect(await screen.findByText('Biology 101')).toBeInTheDocument();
    // The code is the thing a teacher hands out, so it is on the row.
    expect(screen.getByText('ABC234')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Create class' })).toBeInTheDocument();
    // Counts read as words, not bare numbers.
    expect(screen.getByText(/2 students · 1 quiz/)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Join class' })).not.toBeInTheDocument();
  });

  it('gives a student the join box and the classes they are in, not a create form', async () => {
    vi.mocked(api.getEnrolledClassrooms).mockResolvedValue([{ id: CLASS_ID, name: 'Biology 101' }]);

    renderDashboard('Student');

    expect(await screen.findByText('Biology 101')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Join class' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Create class' })).not.toBeInTheDocument();
    // A student is never shown a join code: they receive one, they do not issue one.
    expect(screen.queryByText('Join code')).not.toBeInTheDocument();
  });

  it('tells a new teacher what to do first', async () => {
    renderDashboard('Teacher');
    expect(await screen.findByText(/No classes yet/)).toBeInTheDocument();
  });

  it('tells a new student what to do first', async () => {
    renderDashboard('Student');
    expect(await screen.findByText(/not in any classes yet/i)).toBeInTheDocument();
  });

  it('joins by code and reports it in words', async () => {
    vi.mocked(api.joinClassroom).mockResolvedValue({ classroomId: CLASS_ID, name: 'Biology 101' });
    const user = userEvent.setup();
    renderDashboard('Student');

    await user.type(await screen.findByLabelText(/class code/i), 'abc234');
    await user.click(screen.getByRole('button', { name: 'Join class' }));

    // Typed lowercase, sent uppercase: a code read off a board should not fail on case.
    expect(vi.mocked(api.joinClassroom)).toHaveBeenCalledWith('ABC234');
    expect(await screen.findByText(/You joined Biology 101\./)).toBeInTheDocument();
  });

  it('explains a code that opens nothing, on the field rather than as a toast', async () => {
    const notFound = Object.assign(new Error('nope'), { name: 'ApiError', status: 404 });
    Object.setPrototypeOf(notFound, (await import('@/lib/api/errors')).ApiError.prototype);
    vi.mocked(api.joinClassroom).mockRejectedValue(notFound);

    const user = userEvent.setup();
    renderDashboard('Student');

    await user.type(await screen.findByLabelText(/class code/i), 'ZZZZZZ');
    await user.click(screen.getByRole('button', { name: 'Join class' }));

    expect(await screen.findByText(/doesn't open any class/i)).toBeInTheDocument();
  });

  it('has no accessibility violations for either role', async () => {
    vi.mocked(api.getOwnedClassrooms).mockResolvedValue([
      {
        id: CLASS_ID,
        name: 'Biology 101',
        joinCode: 'ABC234',
        studentCount: 1,
        quizCount: 0,
        createdAt: '2026-07-19T00:00:00Z',
        archivedAt: null,
      },
    ]);

    const teacher = renderDashboard('Teacher');
    await screen.findByText('Biology 101');
    expect(await axe(teacher.container)).toHaveNoViolations();
    teacher.unmount();

    const student = renderDashboard('Student');
    // Wait for the list query to settle, not just for the form to paint: the code field renders
    // immediately, so asserting on it races the query and updates state outside act().
    await screen.findByText(/not in any classes yet/i);
    expect(await axe(student.container)).toHaveNoViolations();
  });
});
