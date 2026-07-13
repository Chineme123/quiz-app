import { CaretDown } from '@phosphor-icons/react';
import { Icon } from '@/components/ui/Icon';
import { FAQ } from '../content';
import { Reveal } from '../motion/motion';

/** Frequently asked questions as a native <details> accordion: works with no JavaScript,
 *  which the prerendered page (AC-11) and keyboard users both benefit from. */
export function Faq() {
  return (
    <section id="faq" className="qz-section qz-faq" aria-labelledby="faq-title">
      <div className="qz-container qz-faq__inner">
        <Reveal className="qz-section__head">
          <span className="qz-eyebrow">Questions</span>
          <h2 id="faq-title" className="qz-section__title">
            Good questions, honest answers.
          </h2>
        </Reveal>
        <ul className="qz-faq__list">
          {FAQ.map((item, index) => (
            <Reveal as="li" key={item.q} delay={index * 0.06}>
              <details className="qz-faq__item">
                <summary className="qz-faq__q">
                  <span>{item.q}</span>
                  <span className="qz-faq__chevron" aria-hidden="true">
                    <Icon icon={CaretDown} weight="bold" />
                  </span>
                </summary>
                <p className="qz-faq__a">{item.a}</p>
              </details>
            </Reveal>
          ))}
        </ul>
      </div>
    </section>
  );
}
