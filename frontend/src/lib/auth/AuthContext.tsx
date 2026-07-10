import { useCallback, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { tokenStore } from './tokenStore';
import { AuthContext } from './useAuth';
import type { AuthStatus, AuthUser } from './useAuth';
import type { Role } from './session';
import { refreshSession, broadcastSignOut, broadcastToken } from '@/lib/api/refresh';
import * as authApi from '@/features/auth/auth.api';

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [status, setStatus] = useState<AuthStatus>('loading');

  // Boot: exchange the HttpOnly refresh cookie for an access token. This is what
  // makes an in-memory token survive a full page reload.
  useEffect(() => {
    let cancelled = false;
    void refreshSession().then((session) => {
      if (cancelled) return;
      if (session) {
        setUser({ userId: session.userId, role: session.role });
        setStatus('authenticated');
      } else {
        setStatus('anonymous');
      }
    });
    return () => {
      cancelled = true;
    };
  }, []);

  // If another tab signs out, the token store is cleared here too; drop the user.
  useEffect(() => {
    return tokenStore.subscribe((token) => {
      if (token === null) {
        setUser(null);
        setStatus('anonymous');
      }
    });
  }, []);

  const signIn = useCallback(async (email: string, password: string) => {
    const session = await authApi.login(email, password);
    tokenStore.set(session.token);
    broadcastToken(session.token);
    setUser({ userId: session.userId, role: session.role, email });
    setStatus('authenticated');
  }, []);

  const register = useCallback(async (email: string, password: string, role: Role) => {
    const session = await authApi.register(email, password, role);
    tokenStore.set(session.token);
    broadcastToken(session.token);
    setUser({ userId: session.userId, role: session.role, email });
    setStatus('authenticated');
  }, []);

  const signOut = useCallback(async () => {
    try {
      await authApi.logout();
    } finally {
      tokenStore.clear();
      broadcastSignOut();
      setUser(null);
      setStatus('anonymous');
    }
  }, []);

  const value = useMemo(
    () => ({ user, status, signIn, register, signOut }),
    [user, status, signIn, register, signOut],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
