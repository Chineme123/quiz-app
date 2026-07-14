import { useRef, useState } from 'react';
import type { KeyboardEvent } from 'react';
import { motion } from 'framer-motion';
import { Link } from 'react-router';
import { HERO } from '../content';
import type { Audience } from '../content';
import { Blob, Bubble, Dots, Highlight } from '../decoration/Decoration';
import { AmbientFloat, Parallax } from '../motion/motion';
import { useMotionReady } from '../motion/useMotionReady';
import studentFemale from '../assets/personas/student-female.jpg';
import professor from '../assets/personas/professor.jpg';

const IMAGES: Record<Audience, string> = {
  students: studentFemale,
  teachers: professor,
};
const ORDER: readonly Audience[] = ['students', 'teachers'];

/**
 * The hero with a "For students / For teachers" audience toggle, built as an ARIA
 * tabs widget (AC-2): each toggle is a tab, the hero body is the tab panel. Arrow
 * keys move between tabs; the selected tab is announced, the panel is labelled by
 * the active tab, and a polite live region announces that the hero content changed
 * too. No auto rotation. Switching crossfades the copy and the persona (AC-10), and
 * the one blueberry gradient the system allows lives in the visual glow.
 */
export function Hero() {
  const [audience, setAudience] = useState<Audience>('students');
  const tabRefs = useRef<Record<Audience, HTMLButtonElement | null>>({
    students: null,
    teachers: null,
  });
  const motionOn = useMotionReady();

  function onTabKeyDown(event: KeyboardEvent<HTMLButtonElement>) {
    if (event.key !== 'ArrowRight' && event.key !== 'ArrowLeft') return;
    event.preventDefault();
    // Two audiences, so either arrow flips to the other one.
    const next: Audience = audience === 'students' ? 'teachers' : 'students';
    setAudience(next);
    tabRefs.current[next]?.focus();
  }

  const content = HERO[audience];
  // Each audience block is keyed, so switching remounts it and it fades in. When
  // motion is off (reduced motion, or the prerender and its hydration) there is no
  // initial state and the swap is instant, so the content is never hidden.
  const fadeDuration = motionOn ? 0.3 : 0;

  return (
    <section className="qz-hero" aria-labelledby="hero-title">
      <div className="qz-container">
        <div role="tablist" aria-label="Who are you here as" className="qz-toggle">
          {ORDER.map((option) => (
            <button
              key={option}
              ref={(el) => {
                tabRefs.current[option] = el;
              }}
              type="button"
              role="tab"
              id={`hero-tab-${option}`}
              aria-selected={audience === option}
              aria-controls="hero-panel"
              tabIndex={audience === option ? 0 : -1}
              className="qz-toggle__btn"
              onClick={() => setAudience(option)}
              onKeyDown={onTabKeyDown}
            >
              {HERO[option].tabLabel}
            </button>
          ))}
        </div>

        <div className="qz-hero__grid">
          <div
            role="tabpanel"
            id="hero-panel"
            aria-labelledby={`hero-tab-${audience}`}
            tabIndex={0}
            className="qz-hero__content"
          >
            <motion.div
              key={audience}
              initial={motionOn ? { opacity: 0, y: 8 } : false}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: fadeDuration, ease: 'easeOut' }}
            >
              <span className="qz-eyebrow qz-hero__eyebrow">{content.eyebrow}</span>
              <h1 id="hero-title" className="qz-hero__title">
                {content.headlineLead}
                <Highlight>{content.headlineMark}</Highlight>
                {content.headlineTail}
              </h1>
              <p className="qz-hero__sub">{content.subcopy}</p>
              <div className="qz-hero__ctas">
                <Link to={content.primaryCta.to} className="qz-btn qz-btn--accent qz-btn--lg">
                  {content.primaryCta.label}
                </Link>
                <a href={content.secondaryCta.to} className="qz-btn qz-btn--ghost qz-btn--lg">
                  {content.secondaryCta.label}
                </a>
              </div>
            </motion.div>
          </div>

          <div className="qz-hero__visual">
            <div className="qz-hero__glow" aria-hidden="true" />
            <div className="qz-persona-frame">
              <motion.img
                key={audience}
                className="qz-persona"
                src={IMAGES[audience]}
                alt={content.imageAlt}
                width={800}
                height={1000}
                initial={motionOn ? { opacity: 0 } : false}
                animate={{ opacity: 1 }}
                transition={{ duration: fadeDuration }}
              />
            </div>
            <AmbientFloat className="qz-deco" style={{ top: '0%', left: '-4%', zIndex: 2 }} duration={7} range={12}>
              <Bubble tone="coral" size={66} />
            </AmbientFloat>
            <AmbientFloat
              className="qz-deco"
              style={{ bottom: '10%', right: '0%', zIndex: 2 }}
              duration={9}
              range={9}
              delay={0.6}
            >
              <Bubble tone="blueberry" size={40} />
            </AmbientFloat>
            <AmbientFloat
              className="qz-deco"
              style={{ top: '6%', right: '8%', zIndex: 2 }}
              duration={8}
              range={7}
              delay={0.3}
            >
              <Dots tone="blueberry" style={{ width: '2.4rem' }} />
            </AmbientFloat>
          </div>
        </div>
      </div>

      {/* Announce the hero content change to assistive tech (AC-2), beyond the tab's own state. */}
      <p className="qz-sr-only" aria-live="polite">{`Showing Quiztin for ${audience}.`}</p>

      {/* The two big background blobs drift against the scroll, in opposite directions,
          so the hero gains depth as the page moves. */}
      <Parallax className="qz-deco" style={{ width: '26rem', top: '-7rem', right: '-9rem' }} distance={80}>
        <Blob tone="coral" variant={1} />
      </Parallax>
      <Parallax className="qz-deco" style={{ width: '22rem', bottom: '-11rem', left: '-9rem' }} distance={-60}>
        <Blob tone="blueberry" variant={2} />
      </Parallax>
    </section>
  );
}
