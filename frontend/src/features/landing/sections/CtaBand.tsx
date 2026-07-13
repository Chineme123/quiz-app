import { Link } from 'react-router';
import { CTA_BAND } from '../content';
import { Blob } from '../decoration/Decoration';

/** The closing call to action, with the free for classrooms line (AC-3). */
export function CtaBand() {
  const { freeLine, title, body, primary, secondary } = CTA_BAND;
  return (
    <section className="qz-section qz-cta" aria-labelledby="cta-title">
      <div className="qz-container">
        <div className="qz-cta__panel">
          <Blob tone="coral" variant={0} className="qz-deco" style={{ width: '18rem', top: '-5rem', right: '-4rem' }} />
          <Blob tone="blueberry" variant={2} className="qz-deco" style={{ width: '15rem', bottom: '-6rem', left: '-4rem' }} />
          <span className="qz-cta__free">{freeLine}</span>
          <h2 id="cta-title" className="qz-cta__title">
            {title}
          </h2>
          <p className="qz-cta__body">{body}</p>
          <div className="qz-cta__actions">
            <Link to={primary.to} className="qz-btn qz-btn--accent qz-btn--lg">
              {primary.label}
            </Link>
            <Link to={secondary.to} className="qz-btn qz-btn--secondary qz-btn--lg">
              {secondary.label}
            </Link>
          </div>
        </div>
      </div>
    </section>
  );
}
