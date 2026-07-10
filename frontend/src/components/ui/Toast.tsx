import { useCallback, useRef, useState } from 'react';
import type { ReactNode } from 'react';
import { CheckCircle, Info, Warning, WarningCircle, Sparkle, X } from '@phosphor-icons/react';
import { Icon } from './Icon';
import { Button } from './Button';
import { ToastContext } from './useToast';
import type { ToastOptions, ToastTone } from './useToast';
import './Toast.css';

const TONE_ICON = {
  info: Info,
  success: CheckCircle,
  danger: WarningCircle,
  warning: Warning,
  ai: Sparkle,
} as const;

export interface ToastProps {
  tone?: ToastTone;
  title?: string;
  message: string;
  onClose?: () => void;
}

/** A single notification. danger/warning announce assertively (role=alert). */
export function Toast({ tone = 'info', title, message, onClose }: ToastProps) {
  const role = tone === 'danger' || tone === 'warning' ? 'alert' : 'status';
  return (
    <div className={`qz-toast qz-toast--${tone}`} role={role}>
      <span className="qz-toast__icon" aria-hidden="true">
        <Icon icon={TONE_ICON[tone]} weight="fill" />
      </span>
      <div className="qz-toast__body">
        {title && <p className="qz-toast__title">{title}</p>}
        <p className="qz-toast__msg">{message}</p>
      </div>
      {onClose && (
        <Button variant="ghost" size="sm" icon={X} onClick={onClose} aria-label="Dismiss" className="qz-toast__close" />
      )}
    </div>
  );
}

interface ActiveToast extends ToastOptions {
  id: number;
}

export interface ToastProviderProps {
  children: ReactNode;
}

/**
 * Provides useToast().show(). Renders a persistent polite live region so
 * newly-added toasts are announced. Auto-dismisses after `duration`.
 */
export function ToastProvider({ children }: ToastProviderProps) {
  const [toasts, setToasts] = useState<ActiveToast[]>([]);
  const nextId = useRef(0);

  const dismiss = useCallback((id: number) => {
    setToasts((current) => current.filter((t) => t.id !== id));
  }, []);

  const show = useCallback(
    (options: ToastOptions) => {
      const id = nextId.current++;
      setToasts((current) => [...current, { ...options, id }]);
      const duration = options.duration ?? 4000;
      if (duration > 0) {
        setTimeout(() => dismiss(id), duration);
      }
    },
    [dismiss],
  );

  return (
    <ToastContext.Provider value={{ show }}>
      {children}
      <div className="qz-toast-region" aria-live="polite" aria-relevant="additions">
        {toasts.map((t) => (
          <Toast key={t.id} tone={t.tone} title={t.title} message={t.message} onClose={() => dismiss(t.id)} />
        ))}
      </div>
    </ToastContext.Provider>
  );
}
