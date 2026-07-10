import { createContext, useContext } from 'react';
import type { Role } from './session';

export type AuthStatus = 'loading' | 'authenticated' | 'anonymous';

export interface AuthUser {
  userId: string;
  role: Role;
  /** Known only after login/register (from the form), not after a boot refresh. */
  email?: string;
}

export interface AuthContextValue {
  user: AuthUser | null;
  status: AuthStatus;
  signIn: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string, role: Role) => Promise<void>;
  signOut: () => Promise<void>;
}

export const AuthContext = createContext<AuthContextValue | null>(null);

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within an <AuthProvider>.');
  return ctx;
}
