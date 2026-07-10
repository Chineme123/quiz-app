import { forwardRef } from 'react';
import type { HTMLAttributes, ReactNode } from 'react';
import './Card.css';

export type CardVariant = 'raised' | 'flat' | 'sunken';
export type CardPadding = 'none' | 'sm' | 'md' | 'lg';

export interface CardProps extends HTMLAttributes<HTMLDivElement> {
  variant?: CardVariant;
  padding?: CardPadding;
  /** Focusable, hover-lifting button surface (e.g. a clickable tile). */
  interactive?: boolean;
  children?: ReactNode;
}

/** The workhorse surface: white, 20px radius, hairline border + soft shadow. */
export const Card = forwardRef<HTMLDivElement, CardProps>(function Card(
  { variant = 'raised', padding = 'md', interactive = false, className, children, ...rest },
  ref,
) {
  const classes = [
    'qz-card',
    `qz-card--${variant}`,
    `qz-card--pad-${padding}`,
    interactive ? 'qz-card--interactive' : '',
    className ?? '',
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <div ref={ref} className={classes} {...(interactive ? { tabIndex: 0, role: 'button' } : {})} {...rest}>
      {children}
    </div>
  );
});
