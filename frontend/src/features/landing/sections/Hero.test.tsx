import { describe, it, expect, beforeEach } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { Hero } from './Hero';
import { setReducedMotion } from '@/test/matchMedia';

// Rendered with reduced motion, so the content swap is instant and fully visible
// (the deterministic path); the animated path is verified in the browser.
function renderHero() {
  return render(
    <MemoryRouter>
      <Hero />
    </MemoryRouter>,
  );
}

describe('Hero audience toggle (AC-2, AC-4)', () => {
  beforeEach(() => {
    setReducedMotion(true);
  });

  it('defaults to the students audience', () => {
    renderHero();
    expect(screen.getByRole('tab', { name: 'For students' })).toHaveAttribute('aria-selected', 'true');
    expect(screen.getByRole('tab', { name: 'For teachers' })).toHaveAttribute('aria-selected', 'false');
    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Learning that');
    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('game');
  });

  it('routes the students call to action to register with the student role hint', () => {
    renderHero();
    expect(screen.getByRole('link', { name: 'Join your class' })).toHaveAttribute('href', '/register?role=Student');
  });

  it('switches the whole hero when For teachers is chosen', async () => {
    const user = userEvent.setup();
    renderHero();

    await user.click(screen.getByRole('tab', { name: 'For teachers' }));

    expect(screen.getByRole('tab', { name: 'For teachers' })).toHaveAttribute('aria-selected', 'true');
    expect(screen.getByRole('tab', { name: 'For students' })).toHaveAttribute('aria-selected', 'false');
    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Write a quiz once');
    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('grading');
    // The teacher call to action hints the teacher role on register (AC-4).
    expect(screen.getByRole('link', { name: 'Create your first quiz' })).toHaveAttribute(
      'href',
      '/register?role=Teacher',
    );
  });

  it('announces the content change to assistive tech, not just the tab state (AC-2)', async () => {
    const user = userEvent.setup();
    const { container } = renderHero();

    const liveRegion = container.querySelector('[aria-live="polite"]');
    expect(liveRegion).toHaveTextContent('Showing Quiztin for students.');

    await user.click(screen.getByRole('tab', { name: 'For teachers' }));
    expect(liveRegion).toHaveTextContent('Showing Quiztin for teachers.');
  });

  it('is operable with the arrow keys', async () => {
    const user = userEvent.setup();
    renderHero();

    const students = screen.getByRole('tab', { name: 'For students' });
    students.focus();
    await user.keyboard('{ArrowRight}');

    const teachers = screen.getByRole('tab', { name: 'For teachers' });
    expect(teachers).toHaveAttribute('aria-selected', 'true');
    expect(teachers).toHaveFocus();
  });

  it('exposes the hero body as a tab panel labelled by the active tab', () => {
    renderHero();
    const panel = screen.getByRole('tabpanel');
    expect(within(panel).getByRole('heading', { level: 1 })).toBeInTheDocument();
    expect(panel).toHaveAttribute('aria-labelledby', 'hero-tab-students');
  });

  it('does not auto rotate: the default audience stays put with no interaction', () => {
    renderHero();
    expect(screen.getByRole('tab', { name: 'For students' })).toHaveAttribute('aria-selected', 'true');
  });
});
