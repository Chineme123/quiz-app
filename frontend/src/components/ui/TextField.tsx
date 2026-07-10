import { forwardRef } from 'react';
import type { InputHTMLAttributes, TextareaHTMLAttributes } from 'react';
import type { Icon as PhosphorIcon } from '@phosphor-icons/react';
import { Field } from './Field';
import type { FieldChromeProps } from './Field';
import { Icon } from './Icon';

type NativeInput = Omit<InputHTMLAttributes<HTMLInputElement>, 'id' | 'className'>;
type NativeTextarea = Omit<TextareaHTMLAttributes<HTMLTextAreaElement>, 'id' | 'className'>;

export interface TextFieldProps extends FieldChromeProps, NativeInput {
  /** Leading icon inside the control. */
  icon?: PhosphorIcon;
  /** Render a <textarea> instead of an <input>. */
  multiline?: boolean;
  rows?: number;
  /** Textarea-only props when multiline. */
  textareaProps?: NativeTextarea;
}

/**
 * Single- or multi-line text control. Forwards its ref to the underlying
 * input/textarea so a form can focus and scroll to it (scroll-to-first-error).
 */
export const TextField = forwardRef<HTMLInputElement | HTMLTextAreaElement, TextFieldProps>(function TextField(
  { label, hint, error, required, optional, id, className, icon, multiline, rows, textareaProps, ...inputProps },
  ref,
) {
  const chrome = { label, hint, error, required, optional, id, className };

  return (
    <Field {...chrome}>
      {(aria) => (
        <div className={`qz-field__control-wrap${icon ? ' qz-field__control-wrap--icon' : ''}`}>
          {icon && (
            <span className="qz-field__lead-icon" aria-hidden="true">
              <Icon icon={icon} />
            </span>
          )}
          {multiline ? (
            <textarea
              {...aria}
              ref={ref as React.Ref<HTMLTextAreaElement>}
              className="qz-field__control qz-field__control--multiline"
              rows={rows ?? 4}
              {...textareaProps}
            />
          ) : (
            <input
              {...aria}
              ref={ref as React.Ref<HTMLInputElement>}
              className="qz-field__control"
              {...inputProps}
            />
          )}
        </div>
      )}
    </Field>
  );
});
