import { Link } from 'react-router';
import { Check } from '@phosphor-icons/react';
import { Icon } from '@/components/ui/Icon';
import { VALUE } from '../content';
import type { ValueId } from '../content';
import { Bubble } from '../decoration/Decoration';
import professorOffice from '../assets/personas/professor-office.jpg';
import studentsTutoring from '../assets/personas/students-tutoring.jpg';

const IMAGES: Record<ValueId, string> = {
  'for-teachers': professorOffice,
  'for-students': studentsTutoring,
};

/** The two audience value blocks (AC-3), alternating text and photo, each with a real scene. */
export function ValueSplit() {
  return (
    <>
      {VALUE.map((block, index) => {
        const imageFirst = index % 2 === 1;
        return (
          <section
            key={block.id}
            id={block.id}
            className={`qz-section qz-value ${imageFirst ? 'qz-value--image-first' : ''}`.trim()}
            aria-labelledby={`${block.id}-title`}
          >
            <div className="qz-container qz-value__grid">
              <div className="qz-value__text">
                <span className="qz-eyebrow">{block.eyebrow}</span>
                <h2 id={`${block.id}-title`} className="qz-section__title">
                  {block.title}
                </h2>
                <p className="qz-section__lead">{block.body}</p>
                <ul className="qz-value__points">
                  {block.points.map((point) => (
                    <li key={point} className="qz-value__point">
                      <span className="qz-value__check">
                        <Icon icon={Check} weight="bold" />
                      </span>
                      <span>{point}</span>
                    </li>
                  ))}
                </ul>
                <Link to={block.cta.to} className="qz-btn qz-btn--accent qz-btn--lg">
                  {block.cta.label}
                </Link>
              </div>

              <div className="qz-value__media">
                <div className="qz-photo-frame">
                  <img
                    className="qz-photo"
                    src={IMAGES[block.id]}
                    alt={block.imageAlt}
                    width={1000}
                    height={750}
                    loading="lazy"
                  />
                </div>
                <Bubble
                  tone={block.audience === 'teachers' ? 'blueberry' : 'coral'}
                  size={54}
                  className="qz-deco"
                  style={{ bottom: '-1.2rem', [imageFirst ? 'right' : 'left']: '-1rem', zIndex: 2 }}
                />
              </div>
            </div>
          </section>
        );
      })}
    </>
  );
}
