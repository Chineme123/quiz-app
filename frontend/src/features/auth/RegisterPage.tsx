import { Navigate } from 'react-router';
import { Card } from '@/components/ui';
import { useAuth } from '@/lib/auth/useAuth';

/** Placeholder. The real register form is build-plan task 6. */
export function RegisterPage() {
  const { status } = useAuth();
  if (status === 'authenticated') return <Navigate to="/profile" replace />;

  return (
    <div className="grid min-h-screen place-items-center bg-bg p-6">
      <Card variant="raised" padding="lg" className="w-full max-w-md text-center">
        <h1 className="font-display text-2xl text-text-strong">Create your account</h1>
        <p className="mt-3 text-text-body">The registration form arrives in the next build.</p>
      </Card>
    </div>
  );
}
