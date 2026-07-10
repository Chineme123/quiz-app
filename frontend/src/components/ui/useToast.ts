import { createContext, useContext } from 'react';

export type ToastTone = 'info' | 'success' | 'danger' | 'warning' | 'ai';

export interface ToastOptions {
  tone?: ToastTone;
  title?: string;
  message: string;
  /** ms before auto-dismiss; 0 keeps it until closed. Default 4000. */
  duration?: number;
}

export interface ToastContextValue {
  show: (options: ToastOptions) => void;
}

export const ToastContext = createContext<ToastContextValue | null>(null);

export function useToast(): ToastContextValue {
  const ctx = useContext(ToastContext);
  if (!ctx) throw new Error('useToast must be used within a <ToastProvider>.');
  return ctx;
}
