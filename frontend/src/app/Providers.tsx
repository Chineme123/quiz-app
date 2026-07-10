import type { ReactNode } from 'react';
import { QueryClientProvider } from '@tanstack/react-query';
import { createQueryClient } from '@/lib/api/queryClient';
import { AuthProvider } from '@/lib/auth/AuthContext';
import { ToastProvider } from '@/components/ui';

const queryClient = createQueryClient();

/** App-wide providers: server state, then auth, then toasts. */
export function Providers({ children }: { children: ReactNode }) {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <ToastProvider>{children}</ToastProvider>
      </AuthProvider>
    </QueryClientProvider>
  );
}
