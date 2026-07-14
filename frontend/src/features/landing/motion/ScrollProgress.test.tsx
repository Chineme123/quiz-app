import { describe, it, expect, beforeEach } from 'vitest';
import { render, fireEvent, waitFor } from '@testing-library/react';
import { ScrollProgress } from './motion';
import { setReducedMotion } from '@/test/matchMedia';

// The motion ALLOWED path. It lives in its own file because framer-motion's
// useReducedMotion caches the media query answer the first time it runs, so a file
// can only exercise one motion mode. The reduced motion path is in motion.test.tsx.
//
// The bar reads the live document geometry on every scroll rather than measuring the
// scrollable range once, because the landing arrives as its own chunk and the page
// keeps growing as images and fonts land.

/** Give the jsdom document a scrollable geometry the progress bar can read. */
function setScrollGeometry(scrollTop: number, scrollHeight: number, clientHeight: number) {
  const doc = document.documentElement;
  Object.defineProperty(doc, 'scrollHeight', { value: scrollHeight, configurable: true });
  Object.defineProperty(doc, 'clientHeight', { value: clientHeight, configurable: true });
  Object.defineProperty(doc, 'scrollTop', { value: scrollTop, writable: true, configurable: true });
}

describe('ScrollProgress with motion allowed', () => {
  beforeEach(() => {
    setReducedMotion(false);
  });

  it('renders an aria-hidden bar that is empty at the top of the page', async () => {
    setScrollGeometry(0, 5000, 1000);
    const { container } = render(<ScrollProgress />);

    const bar = container.querySelector('.qz-scroll-progress') as HTMLElement;
    expect(bar).toBeInTheDocument();
    expect(bar).toHaveAttribute('aria-hidden', 'true');
    await waitFor(() => {
      expect(bar.style.transform).toContain('scaleX(0)');
    });
  });

  it('fills as the page scrolls', async () => {
    setScrollGeometry(0, 5000, 1000);
    const { container } = render(<ScrollProgress />);
    const bar = container.querySelector('.qz-scroll-progress') as HTMLElement;

    // Halfway down the 4000px of scrollable range.
    setScrollGeometry(2000, 5000, 1000);
    fireEvent.scroll(window);
    await waitFor(() => {
      expect(bar.style.transform).toContain('scaleX(0.5)');
    });

    // Three quarters of the way.
    setScrollGeometry(3000, 5000, 1000);
    fireEvent.scroll(window);
    await waitFor(() => {
      expect(bar.style.transform).toContain('scaleX(0.75)');
    });

    // All the way to the bottom. A full scaleX(1) is the identity transform, which
    // is written out as "none", so a full bar reads as no transform at all.
    setScrollGeometry(4000, 5000, 1000);
    fireEvent.scroll(window);
    await waitFor(() => {
      expect(bar.style.transform).toBe('none');
    });
  });
});
