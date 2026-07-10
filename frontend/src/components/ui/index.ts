// The design-system primitives, authored from design-system/HANDOFF.md.
//
// Built for the foundation spec (0001): the minimum set the auth + profile
// screens need. The other 11 registry components are deliberately NOT built
// yet and stay `⬜ planned` in context/ui-registry.md:
//   Checkbox, Radio, Switch, IconButton, Badge, ProgressBar, Tooltip, Dialog,
//   Tabs, AnswerChoice, AIFeedbackCard, ResultSummary.
// Add them when a screen that needs them is built, not before.

export { Icon } from './Icon';
export type { IconProps } from './Icon';

export { Button } from './Button';
export type { ButtonProps, ButtonVariant, ButtonSize } from './Button';

export { Field } from './Field';
export type { FieldProps, FieldChromeProps, ControlAria } from './Field';

export { TextField } from './TextField';
export type { TextFieldProps } from './TextField';

export { Select } from './Select';
export type { SelectProps, SelectOption } from './Select';

export { Card } from './Card';
export type { CardProps, CardVariant, CardPadding } from './Card';

export { Toast, ToastProvider } from './Toast';
export type { ToastProps, ToastProviderProps } from './Toast';
export { useToast } from './useToast';
export type { ToastOptions, ToastTone, ToastContextValue } from './useToast';
