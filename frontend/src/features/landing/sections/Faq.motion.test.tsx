import { describe, it, expect, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Faq } from './Faq';
import { FAQ } from '../content';
import { setReducedMotion } from '@/test/matchMedia';

// The motion ALLOWED path: React drives the open state so the panel can animate, but
// the element stays a native <details>, so the contract still holds. Its own file
// because framer-motion caches the reduced motion answer once per module.
describe('Faq accordion with motion allowed', () => {
  beforeEach(() => {
    setReducedMotion(false);
  });

  it('still keeps every answer in the markup (AC-11)', () => {
    render(<Faq />);
    for (const item of FAQ) {
      expect(screen.getByText(item.a)).toBeInTheDocument();
    }
  });

  it('is still a native details element, not a JavaScript only widget', () => {
    const { container } = render(<Faq />);
    expect(container.querySelectorAll('details')).toHaveLength(FAQ.length);
    expect(container.querySelectorAll('summary')).toHaveLength(FAQ.length);
  });

  it('opens on click and closes again, driven by React so the panel can animate', async () => {
    const user = userEvent.setup();
    const { container } = render(<Faq />);
    const first = container.querySelector('details') as HTMLDetailsElement;
    expect(first.open).toBe(false);

    const question = screen.getByText(FAQ[0]?.q ?? '');
    await user.click(question);
    expect(first.open).toBe(true);

    // Closing waits for the collapse animation to land before the element is closed.
    await user.click(question);
    await waitFor(() => {
      expect(first.open).toBe(false);
    });
  });
});
