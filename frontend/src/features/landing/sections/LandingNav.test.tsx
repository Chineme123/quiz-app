import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { LandingNav } from './LandingNav';
import { AuthContext } from '@/lib/auth/useAuth';
import type { AuthStatus } from '@/lib/auth/useAuth';
import { makeAuthValue } from '@/test/authValue';

function renderNav(status: AuthStatus) {
  return render(
    <AuthContext.Provider value={makeAuthValue(status)}>
      <MemoryRouter>
        <LandingNav />
      </MemoryRouter>
    </AuthContext.Provider>,
  );
}

describe('LandingNav auth adaptive actions (AC-5)', () => {
  it('shows Sign in and Get started to a signed out visitor', () => {
    renderNav('anonymous');
    expect(screen.getByRole('link', { name: 'Sign in' })).toHaveAttribute('href', '/sign-in');
    expect(screen.getByRole('link', { name: 'Get started' })).toHaveAttribute('href', '/register');
    expect(screen.queryByRole('link', { name: 'Go to dashboard' })).not.toBeInTheDocument();
  });

  it('shows a link into the app instead of Get started to a signed in visitor', () => {
    renderNav('authenticated');
    expect(screen.getByRole('link', { name: 'Go to dashboard' })).toHaveAttribute('href', '/profile');
    expect(screen.queryByRole('link', { name: 'Get started' })).not.toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Sign in' })).not.toBeInTheDocument();
  });
});
