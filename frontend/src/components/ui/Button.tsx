import { forwardRef } from 'react';
import type { ButtonHTMLAttributes, ReactNode } from 'react';
import type { Icon as PhosphorIcon } from '@phosphor-icons/react';
import { Icon } from './Icon';
import './Button.css';

export type ButtonVariant = 'primary' | 'accent' | 'secondary' | 'subtle' | 'ghost' | 'danger';
export type ButtonSize = 'sm' | 'md' | 'lg';

export interface ButtonProps extends Omit<ButtonHTMLAttributes<HTMLButtonElement>, 'className'> {
  variant?: ButtonVariant;
  size?: ButtonSize;
  icon?: PhosphorIcon;
  iconRight?: PhosphorIcon;
  /** Shows a spinner, sets aria-busy, and blocks interaction. */
  loading?: boolean;
  fullWidth?: boolean;
  children?: ReactNode;
  className?: string;
}

/**
 * The one action primitive. One primary per view (foundation/ui-rules).
 * Press shrinks (--press-scale), focus shows the ring, 44px+ target on md/lg.
 */
export const Button = forwardRef<HTMLButtonElement, ButtonProps>(function Button(
  {
    variant = 'primary',
    size = 'md',
    icon,
    iconRight,
    loading = false,
    fullWidth = false,
    disabled,
    children,
    type = 'button',
    className,
    ...rest
  },
  ref,
) {
  const classes = [
    'qz-btn',
    `qz-btn--${variant}`,
    `qz-btn--${size}`,
    fullWidth ? 'qz-btn--block' : '',
    loading ? 'qz-btn--loading' : '',
    className ?? '',
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <button
      ref={ref}
      type={type}
      className={classes}
      disabled={disabled || loading}
      aria-busy={loading || undefined}
      {...rest}
    >
      {loading && <span className="qz-btn-spin" aria-hidden="true" />}
      {icon && !loading && <Icon icon={icon} weight="bold" />}
      {children != null && <span className="qz-btn-label">{children}</span>}
      {iconRight && !loading && <Icon icon={iconRight} weight="bold" />}
    </button>
  );
});
