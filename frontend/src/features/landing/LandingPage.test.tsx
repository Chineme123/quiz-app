import { describe, it, expect, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { axe } from 'vitest-axe';
import { MemoryRouter } from 'react-router';
import { LandingPage } from './LandingPage';
import { AuthContext } from '@/lib/auth/useAuth';
import { makeAuthValue } from '@/test/authValue';
import { setReducedMotion } from '@/test/matchMedia';

function renderPage() {
  return render(
    <AuthContext.Provider value={makeAuthValue('anonymous')}>
      <MemoryRouter>
        <LandingPage />
      </MemoryRouter>
    </AuthContext.Provider>,
  );
}

describe('LandingPage', () => {
  beforeEach(() => {
    setReducedMotion(true);
  });

  it('presents the sections in the specified order (AC-3)', () => {
    renderPage();
    const headings = screen.getAllByRole('heading').map((h) => h.textContent?.trim() ?? '');
    const expectedOrder = [
      'Learning that', // hero
      'Three steps', // how it works
      'Less marking', // value: teachers
      'A quiz that is on your side', // value: students
      'Feedback that sounds like a person', // AI spotlight
      'Good questions', // FAQ
      'Ready to make quizzes feel calmer', // closing call to action
    ];
    const indices = expectedOrder.map((marker) => headings.findIndex((h) => h.includes(marker)));
    expect(indices.every((i) => i >= 0)).toBe(true);
    expect(indices).toEqual([...indices].sort((a, b) => a - b));
  });

  it('shows the free for classrooms line (AC-3)', () => {
    renderPage();
    expect(screen.getByText('Free for your classroom')).toBeInTheDocument();
  });

  it('discloses that the personas are illustrative (AC-15)', () => {
    renderPage();
    expect(screen.getByText(/illustrative and were generated/i)).toBeInTheDocument();
  });

  it('has no accessibility violations (AC-12)', async () => {
    const { container } = renderPage();
    expect(await axe(container)).toHaveNoViolations();
  });
});
