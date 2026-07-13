import { AI_SPOTLIGHT } from '../content';
import { Bubble } from '../decoration/Decoration';
import { Reveal } from '../motion/motion';

/**
 * The AI feedback spotlight. The card on the right is an illustrative mock of
 * Quiztin's feedback, built from the design tokens (AC-9), because the real quiz
 * and results screens are not built yet. It is labelled "Example" so no one reads
 * it as live data, and it uses the coral AI voice (ui-rules §5).
 */
export function AISpotlight() {
  const { eyebrow, title, body, vignette } = AI_SPOTLIGHT;
  return (
    <section className="qz-section qz-ai" aria-labelledby="ai-title">
      <div className="qz-container qz-ai__grid">
        <Reveal className="qz-ai__text">
          <span className="qz-eyebrow">{eyebrow}</span>
          <h2 id="ai-title" className="qz-section__title">
            {title}
          </h2>
          <p className="qz-section__lead">{body}</p>
        </Reveal>

        <Reveal className="qz-ai__demo" delay={0.1}>
          <figure className="qz-fb">
            <figcaption className="qz-fb__tag">Example feedback</figcaption>
            <p className="qz-fb__q">{vignette.question}</p>
            <p className="qz-fb__their">
              <span className="qz-fb__badge">{vignette.status}</span>
              Your answer: {vignette.theirAnswer}
            </p>
            <div className="qz-fb__msg">
              <span className="qz-fb__avatar" aria-hidden="true">
                Q
              </span>
              <p className="qz-fb__text">{vignette.feedback}</p>
            </div>
          </figure>
          <Bubble tone="coral" size={58} className="qz-deco" style={{ top: '-1.4rem', left: '-1.2rem', zIndex: 2 }} />
          <Bubble tone="ai" size={38} className="qz-deco" style={{ bottom: '-1rem', right: '-0.6rem', zIndex: 2 }} />
        </Reveal>
      </div>
    </section>
  );
}
