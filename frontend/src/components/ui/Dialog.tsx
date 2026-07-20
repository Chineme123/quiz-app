import { useEffect, useId, useRef } from 'react';
import type { ReactNode } from 'react';
import { X } from '@phosphor-icons/react';
import { Icon } from './Icon';
import './Dialog.css';

export type DialogTone = 'primary' | 'accent' | 'danger' | 'warning';
export type DialogSize = 'sm' | 'md';

export interface DialogProps {
  open: boolean;
  /** Called by Escape, a backdrop click, and the close button alike. */
  onClose: () => void;
  title?: string;
  description?: string;
  /** A meaningful glyph for the header, e.g. <Icon icon={Archive} weight="fill" />. */
  icon?: ReactNode;
  tone?: DialogTone;
  size?: DialogSize;
  footer?: ReactNode;
  children?: ReactNode;
  closeLabel?: string;
  showClose?: boolean;
  className?: string;
}

// Everything that can hold focus inside the panel, used to keep Tab from wandering out.
const FOCUSABLE =
  'a[href], button:not([disabled]), textarea:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"])';

/**
 * The accessible modal from the design system (`surfaces/Dialog`): Escape, backdrop, and the
 * close button all call `onClose`; the page behind is scroll locked; `role="dialog"` with
 * `aria-modal` and the title and description wired to it.
 *
 * It is the required shape for confirming anything irreversible (`ui-rules.md` §1). Two things
 * the design system's prototype build leaves out are here because production a11y is not
 * optional: focus is trapped inside the panel while it is open, and it returns to whatever was
 * focused before on close, so a keyboard user is never dropped at the top of the page.
 */
export function Dialog({
  open,
  onClose,
  title,
  description,
  icon,
  tone = 'primary',
  size = 'sm',
  footer,
  children,
  closeLabel = 'Close',
  showClose = true,
  className,
}: DialogProps) {
  const panelRef = useRef<HTMLDivElement>(null);
  const restoreFocusRef = useRef<HTMLElement | null>(null);
  const labelId = useId();
  const descId = useId();

  useEffect(() => {
    if (!open) return undefined;

    // Remember where focus came from, so closing puts it back where the user left it.
    restoreFocusRef.current = document.activeElement as HTMLElement | null;

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        onClose();
        return;
      }
      if (event.key !== 'Tab') return;

      const panel = panelRef.current;
      if (!panel) return;

      const focusable = Array.from(panel.querySelectorAll<HTMLElement>(FOCUSABLE));
      if (focusable.length === 0) {
        // Nothing to move to: keep focus on the panel rather than letting it escape.
        event.preventDefault();
        return;
      }

      const first = focusable[0]!;
      const last = focusable[focusable.length - 1]!;
      const active = document.activeElement;

      if (event.shiftKey && (active === first || active === panel)) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && active === last) {
        event.preventDefault();
        first.focus();
      }
    }

    document.addEventListener('keydown', handleKeyDown);
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    const focusTimer = window.setTimeout(() => panelRef.current?.focus(), 0);

    return () => {
      document.removeEventListener('keydown', handleKeyDown);
      document.body.style.overflow = previousOverflow;
      window.clearTimeout(focusTimer);
      restoreFocusRef.current?.focus?.();
    };
  }, [open, onClose]);

  if (!open) return null;

  const classes = ['qz-dialog', `qz-dialog--${size}`, className ?? ''].filter(Boolean).join(' ');

  return (
    <div
      className="qz-dialog__backdrop"
      // Presentational: the scrim is a visual layer, not a control. Every action it offers is
      // reachable by keyboard through Escape and the close button, so it carries no role of
      // its own and nothing is keyboard-only-inaccessible here.
      role="presentation"
      // mousedown, not click: a drag that starts inside the panel and ends on the backdrop
      // should not count as "clicked outside" and close the dialog under the user.
      onMouseDown={(event) => {
        if (event.target === event.currentTarget) onClose();
      }}
    >
      <div
        ref={panelRef}
        className={classes}
        role="dialog"
        aria-modal="true"
        aria-labelledby={title ? labelId : undefined}
        aria-describedby={description ? descId : undefined}
        tabIndex={-1}
      >
        {(title || icon) && (
          <div className="qz-dialog__head">
            {icon && <span className={`qz-dialog__icon qz-dialog__icon--${tone}`}>{icon}</span>}
            <div className="qz-dialog__titles">
              {title && (
                <span className="qz-dialog__title" id={labelId}>
                  {title}
                </span>
              )}
              {description && (
                <span className="qz-dialog__desc" id={descId}>
                  {description}
                </span>
              )}
            </div>
          </div>
        )}

        {showClose && (
          <button type="button" className="qz-dialog__close" aria-label={closeLabel} onClick={onClose}>
            <Icon icon={X} weight="bold" size="18px" />
          </button>
        )}

        {children && <div className="qz-dialog__body">{children}</div>}
        {footer && <div className="qz-dialog__foot">{footer}</div>}
      </div>
    </div>
  );
}
