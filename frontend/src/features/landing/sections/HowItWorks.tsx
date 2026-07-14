import { NotePencil, Student, Sparkle } from '@phosphor-icons/react';
import type { Icon as PhosphorIcon } from '@phosphor-icons/react';
import { Icon } from '@/components/ui/Icon';
import { STEPS } from '../content';
import type { StepKey } from '../content';
import { Bubble, Squiggle } from '../decoration/Decoration';
import { Reveal } from '../motion/motion';

const STEP_ICON: Record<StepKey, PhosphorIcon> = {
  create: NotePencil,
  take: Student,
  feedback: Sparkle,
};

/** The core loop in three steps (AC-3): teacher creates, students take, everyone gets feedback. */
export function HowItWorks() {
  return (
    <section id="how-it-works" className="qz-section qz-how" aria-labelledby="how-title">
      <Squiggle tone="coral" className="qz-deco" style={{ width: '7rem', top: '3rem', left: '7%' }} />
      <Bubble tone="blueberry" size={54} className="qz-deco" style={{ top: '4rem', right: '8%' }} />
      <div className="qz-container">
        <Reveal className="qz-section__head">
          <span className="qz-eyebrow">How it works</span>
          <h2 id="how-title" className="qz-section__title">
            Three steps, start to finish.
          </h2>
          <p className="qz-section__lead">
            From a blank quiz to feedback in students' hands, the whole loop stays quick and calm.
          </p>
        </Reveal>
        <ol className="qz-steps">
          {STEPS.map((step, index) => (
            <Reveal as="li" key={step.key} className="qz-step" delay={index * 0.08}>
              <span className="qz-step__n" aria-hidden="true">
                {step.n}
              </span>
              <div className="qz-step__badge">
                <Icon icon={STEP_ICON[step.key]} weight="bold" />
              </div>
              <h3 className="qz-step__title">{step.title}</h3>
              <p className="qz-step__body">{step.body}</p>
            </Reveal>
          ))}
        </ol>
      </div>
    </section>
  );
}
