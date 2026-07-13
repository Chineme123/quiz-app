import { useRef, useState } from 'react';
import type { KeyboardEvent } from 'react';
import { Link } from 'react-router';
import { HERO } from '../content';
import type { Audience } from '../content';
import { Blob, Bubble, Dots, Highlight } from '../decoration/Decoration';
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
 * keys move between tabs; the selected tab is announced, and the panel is labelled
 * by the active tab so the content change is exposed to assistive tech. No auto
 * rotation. The one blueberry gradient the system allows lives in the visual glow.
 */
export function Hero() {
  const [audience, setAudience] = useState<Audience>('students');
  const tabRefs = useRef<Record<Audience, HTMLButtonElement | null>>({
    students: null,
    teachers: null,
  });

  function onTabKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (event.key !== 'ArrowRight' && event.key !== 'ArrowLeft') return;
    event.preventDefault();
    // Two audiences, so either arrow flips to the other one.
    const next: Audience = audience === 'students' ? 'teachers' : 'students';
    setAudience(next);
    tabRefs.current[next]?.focus();
  }

  const content = HERO[audience];

  return (
    <section className="qz-hero" aria-labelledby="hero-title">
      <div className="qz-container">
        <div role="tablist" aria-label="Who are you here as" className="qz-toggle" onKeyDown={onTabKeyDown}>
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
          </div>

          <div className="qz-hero__visual">
            <div className="qz-hero__glow" aria-hidden="true" />
            <div className="qz-persona-frame">
              <img
                className="qz-persona"
                src={IMAGES[audience]}
                alt={content.imageAlt}
                width={800}
                height={1000}
              />
            </div>
            <Bubble tone="coral" size={66} className="qz-deco" style={{ top: '0%', left: '-4%', zIndex: 2 }} />
            <Bubble tone="blueberry" size={40} className="qz-deco" style={{ bottom: '10%', right: '0%', zIndex: 2 }} />
            <Dots tone="blueberry" className="qz-deco" style={{ width: '2.4rem', top: '6%', right: '8%', zIndex: 2 }} />
          </div>
        </div>
      </div>

      <Blob tone="coral" variant={1} className="qz-deco" style={{ width: '26rem', top: '-7rem', right: '-9rem' }} />
      <Blob tone="blueberry" variant={2} className="qz-deco" style={{ width: '22rem', bottom: '-11rem', left: '-9rem' }} />
    </section>
  );
}
