import { useId } from 'react';
import type { CSSProperties, ReactNode } from 'react';

/**
 * The Quiztin decoration kit: soft, on brand SVG shapes that give every landing
 * section its playful "bubbly" texture. Everything here is decorative, so every
 * node is aria-hidden, and every colour is a semantic design token (no raw hex),
 * kept light so it never competes with the text in front of it (AC-6, AC-7).
 *
 * Tones map to the palette's soft aliases so the shapes read as gentle tints.
 */
export type Tone = 'coral' | 'blueberry' | 'sand' | 'ai';

const TONE_FILL: Record<Tone, string> = {
  coral: 'var(--accent-soft)',
  blueberry: 'var(--primary-soft)',
  sand: 'var(--surface-sunken)',
  ai: 'var(--ai-surface)',
};

const TONE_STROKE: Record<Tone, string> = {
  coral: 'var(--accent)',
  blueberry: 'var(--primary)',
  sand: 'var(--border-strong)',
  ai: 'var(--ai-accent)',
};

interface ShapeProps {
  className?: string;
  style?: CSSProperties;
  tone?: Tone;
}

/** One soft organic blob. Three path variants keep clusters from looking stamped. */
export function Blob({ className, style, tone = 'coral', variant = 0 }: ShapeProps & { variant?: 0 | 1 | 2 }) {
  const paths = [
    'M43.9-58.6C56 -50 63.4 -34.6 66.8 -18.7C70.2 -2.7 69.6 13.9 62.4 27.4C55.2 40.9 41.4 51.3 26 57.9C10.6 64.5 -6.5 67.2 -22.6 62.9C-38.7 58.6 -53.8 47.3 -61.6 32.5C-69.4 17.7 -69.9 -0.6 -64.4 -16.4C-58.9 -32.2 -47.4 -45.6 -33.6 -53.9C-19.8 -62.2 -3.7 -65.5 11.6 -64.4C26.9 -63.3 31.8 -67.1 43.9 -58.6Z',
    'M39.5-52.4C50.3 -44 57.4 -30.9 61.7 -16.6C66 -2.3 67.5 13.2 61.9 25.7C56.3 38.2 43.6 47.7 29.6 54.4C15.6 61.1 0.3 65 -15.8 63.1C-31.9 61.2 -48.8 53.5 -58.3 40.4C-67.8 27.3 -69.9 8.8 -66 -8C-62.1 -24.8 -52.2 -39.9 -39 -48.5C-25.8 -57.1 -9.3 -59.2 5.6 -60.6C20.5 -62 28.7 -60.8 39.5 -52.4Z',
    'M48.2-60.8C61.4 -52.3 69.9 -35.6 71.8 -18.7C73.7 -1.8 69 15.3 60.2 29.8C51.4 44.3 38.5 56.2 23.2 62.3C7.9 68.4 -9.8 68.7 -25.9 63C-42 57.3 -56.5 45.6 -63.7 30.6C-70.9 15.6 -70.8 -2.7 -64.9 -18.2C-59 -33.7 -47.3 -46.4 -33.6 -54.6C-19.9 -62.8 -4.2 -66.5 11.7 -66.4C27.6 -66.3 35 -69.3 48.2 -60.8Z',
  ];
  return (
    <svg viewBox="-80 -80 160 160" className={className} style={style} aria-hidden="true" focusable="false">
      <path d={paths[variant]} fill={TONE_FILL[tone]} />
    </svg>
  );
}

/** A glossy floating bubble: a soft fill with a light highlight, like a real bubble. */
export function Bubble({ className, style, tone = 'blueberry', size = 48 }: ShapeProps & { size?: number }) {
  const id = `bub${useId()}`;
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 64 64"
      className={className}
      style={style}
      aria-hidden="true"
      focusable="false"
    >
      <defs>
        <radialGradient id={id} cx="35%" cy="30%" r="75%">
          <stop offset="0%" stopColor="var(--surface-card)" stopOpacity="0.9" />
          <stop offset="55%" stopColor={TONE_FILL[tone]} stopOpacity="0.85" />
          <stop offset="100%" stopColor={TONE_STROKE[tone]} stopOpacity="0.45" />
        </radialGradient>
      </defs>
      <circle cx="32" cy="32" r="30" fill={`url(#${id})`} />
      <circle cx="23" cy="21" r="6" fill="var(--surface-card)" opacity="0.8" />
    </svg>
  );
}

/** A hand drawn squiggle, for accents and step connectors. */
export function Squiggle({ className, style, tone = 'coral' }: ShapeProps) {
  return (
    <svg viewBox="0 0 120 24" className={className} style={style} aria-hidden="true" focusable="false">
      <path
        d="M2 12C12 2 22 2 32 12S52 22 62 12 82 2 92 12s20 10 26 0"
        fill="none"
        stroke={TONE_STROKE[tone]}
        strokeWidth="3.5"
        strokeLinecap="round"
      />
    </svg>
  );
}

/** A small scatter of dots, the confetti of the kit. */
export function Dots({ className, style, tone = 'blueberry' }: ShapeProps) {
  const pts = [
    [6, 6],
    [22, 14],
    [10, 28],
    [30, 32],
    [4, 44],
    [26, 50],
  ];
  return (
    <svg viewBox="0 0 40 56" className={className} style={style} aria-hidden="true" focusable="false">
      {pts.map(([cx, cy], i) => (
        <circle key={i} cx={cx} cy={cy} r="3" fill={TONE_STROKE[tone]} opacity="0.55" />
      ))}
    </svg>
  );
}

/** A full width wavy divider that sits between sections. */
export function WaveDivider({
  className,
  fill = 'var(--color-bg)',
  flip = false,
}: {
  className?: string;
  fill?: string;
  flip?: boolean;
}) {
  return (
    <svg
      viewBox="0 0 1440 80"
      preserveAspectRatio="none"
      className={className}
      style={flip ? { transform: 'scaleY(-1)' } : undefined}
      aria-hidden="true"
      focusable="false"
    >
      <path d="M0 40C240 78 480 78 720 48S1200 4 1440 32V80H0Z" fill={fill} />
    </svg>
  );
}

/** A coral highlighter swipe behind a word, the "fun academic" note from the inspo. */
export function Highlight({ children, className }: { children: ReactNode; className?: string }) {
  return <mark className={`qz-mark ${className ?? ''}`.trim()}>{children}</mark>;
}
