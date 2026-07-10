import { forwardRef } from 'react';
import type { SelectHTMLAttributes, ReactNode } from 'react';
import { CaretDown } from '@phosphor-icons/react';
import { Field } from './Field';
import type { FieldChromeProps } from './Field';
import { Icon } from './Icon';

export interface SelectOption {
  value: string;
  label: string;
}

type NativeSelect = Omit<SelectHTMLAttributes<HTMLSelectElement>, 'id' | 'className'>;

export interface SelectProps extends FieldChromeProps, NativeSelect {
  /** Options as data, or pass <option> children instead. */
  options?: SelectOption[];
  /** A disabled, selected-by-default first option. */
  placeholder?: string;
  children?: ReactNode;
}

/** Styled native <select> with a caret. Native = free keyboard + mobile behaviour. */
export const Select = forwardRef<HTMLSelectElement, SelectProps>(function Select(
  { label, hint, error, required, optional, id, className, options, placeholder, children, value, defaultValue, ...selectProps },
  ref,
) {
  const chrome = { label, hint, error, required, optional, id, className };
  // A placeholder is only valid when the control is empty by default.
  const showPlaceholder = placeholder != null && value === undefined && defaultValue === undefined;

  return (
    <Field {...chrome}>
      {(aria) => (
        <div className="qz-select-wrap">
          <select
            {...aria}
            ref={ref}
            className="qz-select"
            value={value}
            defaultValue={showPlaceholder ? '' : defaultValue}
            {...selectProps}
          >
            {placeholder != null && (
              <option value="" disabled>
                {placeholder}
              </option>
            )}
            {options
              ? options.map((o) => (
                  <option key={o.value} value={o.value}>
                    {o.label}
                  </option>
                ))
              : children}
          </select>
          <span className="qz-select__caret" aria-hidden="true">
            <Icon icon={CaretDown} weight="bold" />
          </span>
        </div>
      )}
    </Field>
  );
});
