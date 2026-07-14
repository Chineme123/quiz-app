import { useState } from 'react';
import type { MouseEvent } from 'react';
import { motion } from 'framer-motion';
import { CaretDown } from '@phosphor-icons/react';
import { Icon } from '@/components/ui/Icon';
import { FAQ } from '../content';
import { Reveal } from '../motion/motion';
import { useMotionReady } from '../motion/useMotionReady';

/**
 * One question. It stays a native <details> element, so with no JavaScript the
 * browser still opens and closes it and every answer is present in the prerendered
 * markup for a crawler (AC-11). When motion is allowed we take the panel over and
 * animate it open and closed (AC-10); when it is not, the native element does the
 * work and nothing animates.
 */
function FaqItem({ q, a }: { q: string; a: string }) {
  const motionOn = useMotionReady();
  const [open, setOpen] = useState(false);
  // The <details open> attribute has to stay true through the closing animation, or
  // the browser hides the panel before it has finished collapsing.
  const [domOpen, setDomOpen] = useState(false);

  const summary = (
    <>
      <span>{q}</span>
      {motionOn ? (
        <motion.span
          className="qz-faq__chevron"
          aria-hidden="true"
          animate={{ rotate: open ? 180 : 0 }}
          transition={{ duration: 0.25, ease: 'easeOut' }}
        >
          <Icon icon={CaretDown} weight="bold" />
        </motion.span>
      ) : (
        <span className="qz-faq__chevron" aria-hidden="true">
          <Icon icon={CaretDown} weight="bold" />
        </span>
      )}
    </>
  );

  if (!motionOn) {
    return (
      <details className="qz-faq__item">
        <summary className="qz-faq__q">{summary}</summary>
        <p className="qz-faq__a">{a}</p>
      </details>
    );
  }

  function onSummaryClick(event: MouseEvent<HTMLElement>) {
    // Take over the native toggle so the panel can animate. This also covers the
    // keyboard, because Enter and Space on a summary dispatch a click.
    event.preventDefault();
    if (open) {
      setOpen(false); // collapse, then domOpen closes it once the animation lands
    } else {
      setDomOpen(true); // reveal the panel box first, then grow it
      setOpen(true);
    }
  }

  return (
    <details className="qz-faq__item" open={domOpen}>
      <summary className="qz-faq__q" onClick={onSummaryClick}>
        {summary}
      </summary>
      <motion.div
        className="qz-faq__panel"
        initial={false}
        animate={{ height: open ? 'auto' : 0, opacity: open ? 1 : 0 }}
        transition={{ duration: 0.3, ease: [0.22, 1, 0.36, 1] }}
        onAnimationComplete={() => {
          if (!open) setDomOpen(false);
        }}
      >
        <p className="qz-faq__a">{a}</p>
      </motion.div>
    </details>
  );
}

/** Frequently asked questions, as an accordion that animates when motion is allowed. */
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
              <FaqItem q={item.q} a={item.a} />
            </Reveal>
          ))}
        </ul>
      </div>
    </section>
  );
}
