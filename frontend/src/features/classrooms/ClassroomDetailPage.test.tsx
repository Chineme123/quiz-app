import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe } from 'vitest-axe';
import { MemoryRouter, Routes, Route } from 'react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ToastProvider } from '@/components/ui';
import { ClassroomDetailPage } from './ClassroomDetailPage';
import * as api from './classrooms.api';

vi.mock('./classrooms.api');

const CLASS_ID = '33333333-0000-0000-0000-000000000003';
const STUDENT_ID = '22222222-0000-0000-0000-000000000002';

function renderDetail() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <MemoryRouter initialEntries={[`/classrooms/${CLASS_ID}`]}>
          <Routes>
            <Route path="classrooms/:classroomId" element={<ClassroomDetailPage />} />
            <Route path="dashboard" element={<h1>Your classes</h1>} />
          </Routes>
        </MemoryRouter>
      </ToastProvider>
    </QueryClientProvider>,
  );
}

function activeClassroom(overrides = {}) {
  return {
    id: CLASS_ID,
    name: 'Biology 101',
    isOwner: true,
    joinCode: 'ABC234',
    archivedAt: null,
    studentCount: 1,
    quizCount: 2,
    ...overrides,
  };
}

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(api.getClassroom).mockResolvedValue(activeClassroom());
  vi.mocked(api.getClassroomRoster).mockResolvedValue({
    items: [{ studentId: STUDENT_ID, enrolledAt: '2026-07-19T00:00:00Z' }],
    total: 1,
    page: 1,
    pageSize: 20,
  });
});

describe('ClassroomDetailPage', () => {
  it('shows the class, its code, and its roster to the owner', async () => {
    renderDetail();

    expect(await screen.findByRole('heading', { name: 'Biology 101' })).toBeInTheDocument();
    expect(screen.getByText('ABC234')).toBeInTheDocument();
    expect(await screen.findByText(STUDENT_ID)).toBeInTheDocument();
  });

  it('never archives on a single tap: it asks first, and cancelling changes nothing', async () => {
    const user = userEvent.setup();
    renderDetail();

    await user.click(await screen.findByRole('button', { name: /archive this class/i }));

    // ui-rules §1: an action that changes what students can reach gets a plain spoken confirm.
    const dialog = await screen.findByRole('dialog');
    expect(dialog).toHaveAccessibleName('Archive this class?');
    expect(vi.mocked(api.archiveClassroom)).not.toHaveBeenCalled();

    await user.click(screen.getByRole('button', { name: 'Keep it open' }));

    expect(vi.mocked(api.archiveClassroom)).not.toHaveBeenCalled();
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('archives once confirmed', async () => {
    vi.mocked(api.archiveClassroom).mockResolvedValue(undefined);
    const user = userEvent.setup();
    renderDetail();

    await user.click(await screen.findByRole('button', { name: /archive this class/i }));
    await user.click(await screen.findByRole('button', { name: 'Yes, archive it' }));

    expect(vi.mocked(api.archiveClassroom)).toHaveBeenCalledWith(CLASS_ID);
    expect(await screen.findByText(/Class archived/i)).toBeInTheDocument();
  });

  it('offers restore, not archive, once the class is put away', async () => {
    vi.mocked(api.getClassroom).mockResolvedValue(
      activeClassroom({ archivedAt: '2026-07-19T22:00:00Z' }),
    );

    renderDetail();

    expect(await screen.findByRole('button', { name: /restore this class/i })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /archive this class/i })).not.toBeInTheDocument();
    // An archived class hands out no code, because nobody can join it.
    expect(screen.queryByText('ABC234')).not.toBeInTheDocument();
  });

  it('asks before removing a student, and says their work is kept', async () => {
    vi.mocked(api.removeStudent).mockResolvedValue(undefined);
    const user = userEvent.setup();
    renderDetail();

    await user.click(await screen.findByRole('button', { name: 'Remove' }));

    const dialog = await screen.findByRole('dialog');
    expect(dialog).toHaveAccessibleName('Remove this student?');
    expect(dialog).toHaveAccessibleDescription(/already submitted is kept/i);
    expect(vi.mocked(api.removeStudent)).not.toHaveBeenCalled();

    await user.click(screen.getByRole('button', { name: 'Yes, remove them' }));
    expect(vi.mocked(api.removeStudent)).toHaveBeenCalledWith(CLASS_ID, STUDENT_ID);
  });

  it('renames using what is in the field, so saving an untouched name works', async () => {
    vi.mocked(api.renameClassroom).mockResolvedValue(undefined);
    const user = userEvent.setup();
    renderDetail();

    // The field is seeded from the loaded class. Submitting without typing must send that name,
    // not an empty string.
    await user.click(await screen.findByRole('button', { name: 'Save name' }));

    expect(vi.mocked(api.renameClassroom)).toHaveBeenCalledWith(CLASS_ID, 'Biology 101');
  });

  it('issues a new code and says the old one stopped working', async () => {
    vi.mocked(api.regenerateJoinCode).mockResolvedValue('XYZ789');
    const user = userEvent.setup();
    renderDetail();

    await user.click(await screen.findByRole('button', { name: 'Issue a new code' }));

    expect(vi.mocked(api.regenerateJoinCode)).toHaveBeenCalledWith(CLASS_ID);
    expect(await screen.findByText(/no longer work/i)).toBeInTheDocument();
  });

  it('shows a plain not-found for a class that is not yours', async () => {
    // The api turns a 404 into null, and the server answers the same whether the class is
    // missing or simply someone else's, so this screen cannot reveal which.
    vi.mocked(api.getClassroom).mockResolvedValue(null);

    renderDetail();

    expect(await screen.findByText(/couldn't find that class/i)).toBeInTheDocument();
  });

  it('has no accessibility violations, with the confirm open', async () => {
    const user = userEvent.setup();
    const { container } = renderDetail();

    await screen.findByRole('heading', { name: 'Biology 101' });
    expect(await axe(container)).toHaveNoViolations();

    await user.click(screen.getByRole('button', { name: /archive this class/i }));
    await screen.findByRole('dialog');
    expect(await axe(container)).toHaveNoViolations();
  });
});
