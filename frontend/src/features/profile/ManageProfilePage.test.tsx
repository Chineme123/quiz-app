import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ManageProfilePage } from './ManageProfilePage';
import { AuthContext } from '@/lib/auth/useAuth';
import { ToastProvider } from '@/components/ui';
import { ValidationError } from '@/lib/api/errors';
import { makeAuthValue } from '@/test/authValue';
import type { Role } from '@/lib/auth/session';
import type { Profile } from './profile.schemas';
import * as api from './profile.api';

vi.mock('./profile.api');

function makeProfile(overrides: Partial<Profile> = {}): Profile {
  return {
    userId: '00000000-0000-0000-0000-000000000001',
    displayName: 'Ada Lovelace',
    bio: null,
    avatarUrl: null,
    school: null,
    department: null,
    academicLevel: 'Senior',
    instructorType: null,
    createdAt: '2026-07-10T00:00:00Z',
    updatedAt: '2026-07-10T00:00:00Z',
    ...overrides,
  };
}

function renderProfile(role: Role, initial: Profile | null) {
  vi.mocked(api.getProfile).mockResolvedValue(initial);
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  render(
    <QueryClientProvider client={queryClient}>
      <AuthContext.Provider value={makeAuthValue('authenticated', role)}>
        <ToastProvider>
          <ManageProfilePage />
        </ToastProvider>
      </AuthContext.Provider>
    </QueryClientProvider>,
  );
}

beforeEach(() => {
  vi.mocked(api.getProfile).mockReset();
  vi.mocked(api.updateProfile).mockReset();
});

describe('ManageProfilePage', () => {
  // AC-10: role decides which conditional field is shown, never both.
  it('shows Academic level to a student and hides Instructor type', async () => {
    renderProfile('Student', null);
    expect(await screen.findByLabelText(/academic level/i)).toBeInTheDocument();
    expect(screen.queryByLabelText(/instructor type/i)).not.toBeInTheDocument();
  });

  it('shows Instructor type to a teacher and hides Academic level', async () => {
    renderProfile('Teacher', null);
    expect(await screen.findByLabelText(/instructor type/i)).toBeInTheDocument();
    expect(screen.queryByLabelText(/academic level/i)).not.toBeInTheDocument();
  });

  // AC-10: a 404 (no row yet) is a blank form, not an error.
  it('renders a blank form for a first-time user without an error', async () => {
    renderProfile('Student', null);
    const displayName = await screen.findByLabelText(/display name/i);
    expect(displayName).toHaveValue('');
    expect(screen.queryByText(/couldn.t load your profile/i)).not.toBeInTheDocument();
  });

  // AC-11: a client validation failure keeps other fields and focuses the first error.
  it('keeps other fields and focuses Display name when it is cleared', async () => {
    const user = userEvent.setup();
    renderProfile('Student', null);
    const displayName = await screen.findByLabelText(/display name/i);
    const school = screen.getByLabelText(/school/i);

    await user.type(displayName, 'Ada Lovelace');
    await user.type(school, 'Analytical Engine University');
    await user.clear(displayName);
    await user.click(screen.getByRole('button', { name: /save changes/i }));

    expect(await screen.findByText('Display name is required.')).toBeInTheDocument();
    expect(school).toHaveValue('Analytical Engine University');
    expect(displayName).toHaveFocus();
    expect(api.updateProfile).not.toHaveBeenCalled();
  });

  // AC-12: the server's string[] validation error is mapped back beneath its field.
  it('maps a server validation error onto the role field', async () => {
    const user = userEvent.setup();
    vi.mocked(api.updateProfile).mockRejectedValue(
      new ValidationError(400, ['AcademicLevel is required for students.']),
    );
    renderProfile('Student', null);

    const displayName = await screen.findByLabelText(/display name/i);
    await user.type(displayName, 'Ada Lovelace');
    // Leave Academic level unselected: the client passes, the server rejects.
    await user.click(screen.getByRole('button', { name: /save changes/i }));

    expect(await screen.findByText('AcademicLevel is required for students.')).toBeInTheDocument();
  });

  // AC-13: a successful save confirms quietly, does not redirect, and keeps the values.
  it('confirms a successful save and leaves the form populated', async () => {
    const user = userEvent.setup();
    vi.mocked(api.updateProfile).mockResolvedValue(makeProfile({ displayName: 'Ada Lovelace' }));
    renderProfile('Student', null);

    const displayName = await screen.findByLabelText(/display name/i);
    await user.type(displayName, 'Ada Lovelace');
    await user.selectOptions(screen.getByLabelText(/academic level/i), 'Senior');
    await user.click(screen.getByRole('button', { name: /save changes/i }));

    expect(await screen.findByText(/your profile has been updated/i)).toBeInTheDocument();
    expect(displayName).toHaveValue('Ada Lovelace');
  });
});
