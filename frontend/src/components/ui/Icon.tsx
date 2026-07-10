import type { Icon as PhosphorIcon, IconWeight } from '@phosphor-icons/react';

export interface IconProps {
  /** A Phosphor icon component, e.g. `import { Check } from '@phosphor-icons/react'`. */
  icon: PhosphorIcon;
  /** regular (idle) · bold (small glyphs) · fill (selected/active). */
  weight?: IconWeight;
  size?: number | string;
  /** Given → the icon is meaningful (role="img" + label). Omitted → decorative (aria-hidden). */
  label?: string;
  className?: string;
}

/**
 * Thin wrapper over Phosphor icons. Decorative by default, so an icon never
 * adds noise to a screen reader unless it carries meaning of its own.
 */
export function Icon({ icon: Glyph, weight = 'regular', size = '1em', label, className }: IconProps) {
  return (
    <Glyph
      weight={weight}
      size={size}
      className={className}
      {...(label ? { role: 'img', 'aria-label': label } : { 'aria-hidden': true })}
    />
  );
}
