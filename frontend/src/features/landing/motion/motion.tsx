import { motion, useMotionValue, useScroll, useTransform } from 'framer-motion';
import { useEffect, useRef } from 'react';
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

/**
 * A thin bar across the top of the viewport that fills as the visitor scrolls the
 * page. Decorative progress feedback, so it is aria-hidden and simply absent when
 * the visitor asked for reduced motion.
 */
export function ScrollProgress() {
  const on = useMotionReady();
  if (!on) return null;
  return <ScrollProgressBar />;
}

// Split out so the scroll hooks only ever run when motion is on (hooks cannot be
// called conditionally, but a component can be rendered conditionally).
function ScrollProgressBar() {
  const progress = useMotionValue(0);

  // The page's scrollable height is not settled when this mounts: the landing arrives
  // as its own chunk and images and fonts land after. framer-motion's window level
  // useScroll measures the range once and would sit at zero, so track it ourselves and
  // re measure whenever the document actually changes size.
  useEffect(() => {
    const doc = document.documentElement;
    let range = 0;

    const update = () => {
      progress.set(range > 0 ? Math.min(1, Math.max(0, doc.scrollTop / range)) : 0);
    };
    const measure = () => {
      range = doc.scrollHeight - doc.clientHeight;
      update();
    };

    measure();
    window.addEventListener('scroll', update, { passive: true });
    window.addEventListener('resize', measure);
    const observer = new ResizeObserver(measure);
    observer.observe(document.body);

    return () => {
      window.removeEventListener('scroll', update);
      window.removeEventListener('resize', measure);
      observer.disconnect();
    };
  }, [progress]);

  return <motion.div className="qz-scroll-progress" style={{ scaleX: progress }} aria-hidden="true" />;
}

interface ParallaxProps {
  children: ReactNode;
  className?: string;
  style?: CSSProperties;
  /** How far the layer drifts against the scroll, in px. Larger reads as further back. */
  distance?: number;
}

/**
 * Drifts a decorative layer against the page scroll, so the page gains depth as it
 * moves. Decorative only (aria-hidden). With motion off it renders a still wrapper in
 * exactly the same place, so nothing shifts for a reduced motion visitor.
 */
export function Parallax({ children, className, style, distance = 60 }: ParallaxProps) {
  const on = useMotionReady();

  if (!on) {
    return (
      <div className={className} style={style} aria-hidden="true">
        {children}
      </div>
    );
  }

  return (
    <ParallaxLayer className={className} style={style} distance={distance}>
      {children}
    </ParallaxLayer>
  );
}

function ParallaxLayer({ children, className, style, distance = 60 }: ParallaxProps) {
  const ref = useRef<HTMLDivElement>(null);
  // Progress from the layer entering the viewport to it leaving the top.
  const { scrollYProgress } = useScroll({ target: ref, offset: ['start end', 'end start'] });
  const y = useTransform(scrollYProgress, [0, 1], [distance, -distance]);

  // The ref sits on the untransformed wrapper so the scroll measurement is not
  // affected by the drift it drives.
  return (
    <div ref={ref} className={className} style={style} aria-hidden="true">
      <motion.div style={{ y }}>{children}</motion.div>
    </div>
  );
}
