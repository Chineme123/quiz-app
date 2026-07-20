import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe } from 'vitest-axe';
import { MemoryRouter, Routes, Route } from 'react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AuthContext } from '@/lib/auth/useAuth';
import { ToastProvider } from '@/components/ui';
import { makeAuthValue } from '@/test/authValue';
import { JoinClassroomPage } from './JoinClassroomPage';
import * as api from './classrooms.api';

vi.mock('./classrooms.api');

const CLASS_ID = '33333333-0000-0000-0000-000000000003';

function renderJoin(code = 'ABC234') {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <AuthContext.Provider value={makeAuthValue('authenticated', 'Student')}>
        <ToastProvider>
          <MemoryRouter initialEntries={[`/join/${code}`]}>
            <Routes>
              <Route path="join/:code" element={<JoinClassroomPage />} />
              <Route path="dashboard" element={<h1>Your classes</h1>} />
            </Routes>
          </MemoryRouter>
        </ToastProvider>
      </AuthContext.Provider>
    </QueryClientProvider>,
  );
}

beforeEach(() => {
  vi.clearAllMocks();
});

describe('JoinClassroomPage', () => {
  it('names the class and waits for a deliberate confirm, never joining on open', async () => {
    vi.mocked(api.previewClassroomByCode).mockResolvedValue({
      classroomId: CLASS_ID,
      name: 'Biology 101',
      alreadyEnrolled: false,
      isOwner: false,
    });

    renderJoin();

    expect(await screen.findByText('Biology 101')).toBeInTheDocument();
    // AC-4: opening the link must not enrol anyone by itself.
    expect(vi.mocked(api.joinClassroom)).not.toHaveBeenCalled();
    expect(screen.getByRole('button', { name: 'Join class' })).toBeInTheDocument();
  });

  it('joins on confirm and sends the student to their classes', async () => {
    vi.mocked(api.previewClassroomByCode).mockResolvedValue({
      classroomId: CLASS_ID,
      name: 'Biology 101',
      alreadyEnrolled: false,
      isOwner: false,
    });
    vi.mocked(api.joinClassroom).mockResolvedValue({ classroomId: CLASS_ID, name: 'Biology 101' });

    const user = userEvent.setup();
    renderJoin();

    await user.click(await screen.findByRole('button', { name: 'Join class' }));

    expect(vi.mocked(api.joinClassroom)).toHaveBeenCalledWith('ABC234');
    expect(await screen.findByText('Your classes')).toBeInTheDocument();
  });

  it('normalizes a lowercase code from the link', async () => {
    vi.mocked(api.previewClassroomByCode).mockResolvedValue({
      classroomId: CLASS_ID,
      name: 'Biology 101',
      alreadyEnrolled: false,
      isOwner: false,
    });

    renderJoin('abc234');

    await screen.findByText('Biology 101');
    expect(vi.mocked(api.previewClassroomByCode)).toHaveBeenCalledWith('ABC234');
  });

  it('says a dead link opens nothing, without hinting whether a class exists', async () => {
    // The api turns both "no such code" and "archived" into null, so the screen cannot tell
    // them apart either. That is the point.
    vi.mocked(api.previewClassroomByCode).mockResolvedValue(null);

    renderJoin('ZZZZZZ');

    expect(await screen.findByText(/doesn't open any class/i)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Join class' })).not.toBeInTheDocument();
  });

  it('offers nothing to join when you are already in the class', async () => {
    vi.mocked(api.previewClassroomByCode).mockResolvedValue({
      classroomId: CLASS_ID,
      name: 'Biology 101',
      alreadyEnrolled: true,
      isOwner: false,
    });

    renderJoin();

    expect(await screen.findByText(/already in this class/i)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Join class' })).not.toBeInTheDocument();
  });

  it('tells a teacher opening their own link that there is nothing to join', async () => {
    vi.mocked(api.previewClassroomByCode).mockResolvedValue({
      classroomId: CLASS_ID,
      name: 'Biology 101',
      alreadyEnrolled: false,
      isOwner: true,
    });

    renderJoin();

    expect(await screen.findByText(/your own class/i)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Join class' })).not.toBeInTheDocument();
  });

  it('has no accessibility violations', async () => {
    vi.mocked(api.previewClassroomByCode).mockResolvedValue({
      classroomId: CLASS_ID,
      name: 'Biology 101',
      alreadyEnrolled: false,
      isOwner: false,
    });

    const { container } = renderJoin();
    await screen.findByText('Biology 101');

    expect(await axe(container)).toHaveNoViolations();
  });
});
