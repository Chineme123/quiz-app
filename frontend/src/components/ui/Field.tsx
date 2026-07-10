import { useId } from 'react';
import type { ReactNode } from 'react';
import { WarningCircle } from '@phosphor-icons/react';
import { Icon } from './Icon';
import './Field.css';

export interface FieldChromeProps {
  label: string;
  hint?: string;
  /** Present → the field is in error: red border, message, aria-invalid. */
  error?: string;
  required?: boolean;
  optional?: boolean;
  /** Override the generated control id (else useId). */
  id?: string;
  className?: string;
}

export interface ControlAria {
  id: string;
  'aria-invalid'?: true;
  'aria-describedby'?: string;
  'aria-required'?: true;
}

export interface FieldProps extends FieldChromeProps {
  /** Render the control, spreading the accessibility props onto it. */
  children: (aria: ControlAria) => ReactNode;
}

/**
 * Shared label + hint + error chrome for form controls. Owns the wiring that is
 * easy to get wrong: a real <label htmlFor>, aria-invalid on error, and
 * aria-describedby pointing at whichever of hint/error is showing.
 */
export function Field({ label, hint, error, required, optional, id, className, children }: FieldProps) {
  const auto = useId();
  const controlId = id ?? auto;
  const hintId = `${controlId}-hint`;
  const errorId = `${controlId}-error`;
  const describedBy = [hint ? hintId : null, error ? errorId : null].filter(Boolean).join(' ') || undefined;

  const aria: ControlAria = {
    id: controlId,
    ...(error ? { 'aria-invalid': true } : {}),
    ...(describedBy ? { 'aria-describedby': describedBy } : {}),
    ...(required ? { 'aria-required': true } : {}),
  };

  return (
    <div className={['qz-field', error ? 'qz-field--error' : '', className ?? ''].filter(Boolean).join(' ')}>
      <label className="qz-field__label" htmlFor={controlId}>
        <span>{label}</span>
        {required && (
          <span className="qz-field__req" aria-hidden="true">
            *
          </span>
        )}
        {optional && !required && <span className="qz-field__opt">Optional</span>}
      </label>

      {children(aria)}

      {hint && !error && (
        <p className="qz-field__hint" id={hintId}>
          {hint}
        </p>
      )}
      {error && (
        <p className="qz-field__error" id={errorId}>
          <Icon icon={WarningCircle} weight="fill" />
          <span>{error}</span>
        </p>
      )}
    </div>
  );
}
