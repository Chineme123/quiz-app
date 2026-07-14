import { describe, it, expect, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Faq } from './Faq';
import { FAQ } from '../content';
import { setReducedMotion } from '@/test/matchMedia';

// The reduced motion path: the plain native <details>, no animation. The motion
// allowed path is in Faq.motion.test.tsx (framer-motion caches the reduced motion
// answer once per module, so one file can only exercise one mode).
describe('Faq accordion with reduced motion', () => {
  beforeEach(() => {
    setReducedMotion(true);
  });

  it('keeps every answer in the markup, so a crawler still reads them (AC-11)', () => {
    render(<Faq />);
    for (const item of FAQ) {
      expect(screen.getByText(item.a)).toBeInTheDocument();
    }
  });

  it('starts closed and opens the question the visitor clicks', async () => {
    const user = userEvent.setup();
    const { container } = render(<Faq />);
    const first = container.querySelector('details') as HTMLDetailsElement;
    expect(first.open).toBe(false);

    await user.click(screen.getByText(FAQ[0]?.q ?? ''));
    expect(first.open).toBe(true);
  });
});
