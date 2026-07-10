import type { AuthContextValue, AuthStatus } from '@/lib/auth/useAuth';
import type { Role } from '@/lib/auth/session';

const FAKE_USER_ID = '00000000-0000-0000-0000-000000000001';

/** Build an AuthContext value for tests that need to render behind the guard or the
 *  app without standing up the real provider (which does a network refresh on mount). */
export function makeAuthValue(status: AuthStatus, role: Role = 'Student'): AuthContextValue {
  return {
    status,
    user: status === 'authenticated' ? { userId: FAKE_USER_ID, role } : null,
    signIn: async () => {},
    register: async () => {},
    signOut: async () => {},
  };
}
