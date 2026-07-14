import { vi } from 'vitest';

/**
 * Point window.matchMedia at a fixed answer for `prefers-reduced-motion`. framer-motion's
 * useReducedMotion reads it, so the landing tests use this to render the deterministic,
 * fully visible reduced motion path (no IntersectionObserver driven reveals to wait on).
 */
export function setReducedMotion(reduce: boolean): void {
  window.matchMedia = vi.fn().mockImplementation((query: string) => ({
    matches: reduce && query.includes('prefers-reduced-motion'),
    media: query,
    onchange: null,
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    addListener: vi.fn(),
    removeListener: vi.fn(),
    dispatchEvent: vi.fn(() => false),
  }));
}
