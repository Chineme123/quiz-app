import { Navigate, Outlet, useLocation } from 'react-router';
import { useAuth } from '@/lib/auth/useAuth';
import { FullPageMessage } from './FullPageMessage';

/**
 * Gate for authenticated routes. While the boot refresh is in flight we wait;
 * an anonymous visitor is sent to sign in, remembering where they were headed.
 */
export function RequireAuth() {
  const { status } = useAuth();
  const location = useLocation();

  if (status === 'loading') return <FullPageMessage title="Loading Quiztin…" />;
  if (status === 'anonymous') {
    return <Navigate to="/sign-in" replace state={{ from: location.pathname }} />;
  }
  return <Outlet />;
}
