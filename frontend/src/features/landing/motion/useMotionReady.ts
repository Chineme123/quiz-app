import { useReducedMotion } from 'framer-motion';
import { useEffect, useLayoutEffect, useState } from 'react';

// useLayoutEffect warns when React runs it on the server. During the build time
// prerender (Node, no window) we fall back to useEffect, which the prerender never
// runs anyway, so no motion state is ever set during prerender.
const useIsoLayoutEffect = typeof window !== 'undefined' ? useLayoutEffect : useEffect;

/**
 * True only when the browser is running (mounted) AND the visitor has not asked for
 * reduced motion. The mount flag flips inside a layout effect, before the browser
 * paints, so a motion component swaps in from its visible resting state with no
 * flash. On the server and on the first client render this is false, which keeps
 * the markup identical across the two and makes hydration clean (spec 0003, AC-10).
 */
export function useMotionReady(): boolean {
  const reduce = useReducedMotion();
  const [mounted, setMounted] = useState(false);
  useIsoLayoutEffect(() => {
    setMounted(true);
  }, []);
  return mounted && !reduce;
}
