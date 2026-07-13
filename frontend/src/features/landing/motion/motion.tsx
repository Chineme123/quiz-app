import { motion } from 'framer-motion';
import type { CSSProperties, ReactNode } from 'react';
import { useMotionReady } from './useMotionReady';

/**
 * The landing page motion kit (spec 0003, AC-10). Every animation on the page runs
 * through here, and every piece obeys one rule: the resting state is the fully
 * visible, static state. So the prerendered HTML (AC-11), a visitor who asked for
 * reduced motion, and the split second before hydration all render content that is
 * already there, with no hidden or shifted state to recover from. The mount and
 * reduced motion gate lives in useMotionReady.
 */

// The elements Reveal knows how to animate. A small fixed map keeps this type safe
// under noUncheckedIndexedAccess, and lets a list item reveal as a real <li>.
const REVEAL_TAGS = {
  div: motion.div,
  li: motion.li,
  section: motion.section,
} as const;
type RevealTag = keyof typeof REVEAL_TAGS;

interface RevealProps {
  children: ReactNode;
  className?: string;
  style?: CSSProperties;
  /** Which element to render (defaults to a div). */
  as?: RevealTag;
  /** Stagger delay in seconds, e.g. index * 0.08 down a list. */
  delay?: number;
}

/**
 * Fades and lifts its children in the first time they scroll into view. At rest it
 * renders a plain, fully visible element, so the prerender, a reduced motion
 * visitor, and the pre hydration paint show the content with no hidden state. The
 * animated version swaps in through the layout effect, before paint, so there is no
 * flash of content appearing and disappearing.
 */
export function Reveal({ children, className, style, as = 'div', delay = 0 }: RevealProps) {
  const on = useMotionReady();

  if (!on) {
    const Tag = as;
    return (
      <Tag className={className} style={style}>
        {children}
      </Tag>
    );
  }

  const MotionTag = REVEAL_TAGS[as];
  return (
    <MotionTag
      className={className}
      style={style}
      initial={{ opacity: 0, y: 18 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, amount: 0.2 }}
      transition={{ duration: 0.5, delay, ease: [0.22, 1, 0.36, 1] }}
    >
      {children}
    </MotionTag>
  );
}

interface AmbientFloatProps {
  children: ReactNode;
  className?: string;
  style?: CSSProperties;
  /** Drift distance in px (the peak of the bob). */
  range?: number;
  /** Seconds for one full up and back cycle. */
  duration?: number;
  delay?: number;
}

/**
 * Wraps a decorative shape in a slow, endless vertical drift, so the bubbles feel
 * alive. Decorative only (aria-hidden). It drifts only when motion is allowed;
 * otherwise it is a still wrapper in the exact same position.
 */
export function AmbientFloat({ children, className, style, range = 10, duration = 6, delay = 0 }: AmbientFloatProps) {
  const on = useMotionReady();

  if (!on) {
    return (
      <div className={className} style={style} aria-hidden="true">
        {children}
      </div>
    );
  }

  return (
    <motion.div
      className={className}
      style={style}
      aria-hidden="true"
      animate={{ y: [0, -range, 0] }}
      transition={{ duration, delay, repeat: Infinity, ease: 'easeInOut' }}
    >
      {children}
    </motion.div>
  );
}
