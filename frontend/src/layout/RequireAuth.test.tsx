import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router';
import { RequireAuth } from './RequireAuth';
import { AuthContext } from '@/lib/auth/useAuth';
import type { AuthStatus } from '@/lib/auth/useAuth';
import { makeAuthValue } from '@/test/authValue';

// AC-9: the guard sends an anonymous visitor to /sign-in, waits during the boot
// refresh, and renders the protected content only when authenticated.
function renderGuard(status: AuthStatus) {
  return render(
    <AuthContext.Provider value={makeAuthValue(status)}>
      <MemoryRouter initialEntries={['/']}>
        <Routes>
          <Route element={<RequireAuth />}>
            <Route index element={<div>Protected content</div>} />
          </Route>
          <Route path="/sign-in" element={<div>Sign in page</div>} />
        </Routes>
      </MemoryRouter>
    </AuthContext.Provider>,
  );
}

describe('RequireAuth', () => {
  it('redirects an anonymous visitor to /sign-in', async () => {
    renderGuard('anonymous');
    expect(await screen.findByText('Sign in page')).toBeInTheDocument();
    expect(screen.queryByText('Protected content')).not.toBeInTheDocument();
  });

  it('waits during the boot refresh instead of flashing sign-in', () => {
    renderGuard('loading');
    expect(screen.getByText('Loading Quiztin…')).toBeInTheDocument();
    expect(screen.queryByText('Sign in page')).not.toBeInTheDocument();
    expect(screen.queryByText('Protected content')).not.toBeInTheDocument();
  });

  it('renders the protected outlet when authenticated', () => {
    renderGuard('authenticated');
    expect(screen.getByText('Protected content')).toBeInTheDocument();
  });
});
