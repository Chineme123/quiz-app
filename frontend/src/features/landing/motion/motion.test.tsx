import { describe, it, expect, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { Reveal, AmbientFloat, ScrollProgress } from './motion';
import { setReducedMotion } from '@/test/matchMedia';

// AC-10: when the visitor asks for reduced motion, nothing animates and all content
// is fully visible and usable. Reveal and AmbientFloat must render their children in
// a plain, visible resting state, with no hidden (opacity 0) or shifted start state.
//
// This file covers the reduced motion path only. framer-motion's useReducedMotion
// caches the media query answer the first time it runs, so a single file cannot test
// both paths; the motion allowed path lives in ScrollProgress.test.tsx.
describe('motion kit under reduced motion (AC-10)', () => {
  beforeEach(() => {
    setReducedMotion(true);
  });

  it('Reveal shows its children with no hidden start state', () => {
    render(
      <Reveal>
        <p data-testid="revealed">Feedback that sounds like a person.</p>
      </Reveal>,
    );
    const content = screen.getByTestId('revealed');
    expect(content).toBeInTheDocument();
    // The wrapper is a plain element, not a motion element parked at opacity 0.
    const wrapper = content.parentElement as HTMLElement;
    expect(wrapper.style.opacity).not.toBe('0');
    expect(wrapper.style.transform).toBe('');
  });

  it('Reveal renders the requested element (a list item stays a real li)', () => {
    render(
      <ul>
        <Reveal as="li">
          <span>Step one</span>
        </Reveal>
      </ul>,
    );
    expect(screen.getByRole('listitem')).toHaveTextContent('Step one');
  });

  it('AmbientFloat still renders its decorative child, without drifting', () => {
    render(
      <AmbientFloat>
        <span data-testid="bubble">deco</span>
      </AmbientFloat>,
    );
    const bubble = screen.getByTestId('bubble');
    expect(bubble).toBeInTheDocument();
    expect((bubble.parentElement as HTMLElement).style.transform).toBe('');
  });

  it('the scroll progress bar is not rendered at all', () => {
    const { container } = render(<ScrollProgress />);
    expect(container.querySelector('.qz-scroll-progress')).toBeNull();
  });
});
