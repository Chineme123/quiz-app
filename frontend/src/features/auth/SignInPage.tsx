import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link, Navigate, useLocation } from 'react-router';
import { EnvelopeSimple, Lock } from '@phosphor-icons/react';
import { Button, TextField } from '@/components/ui';
import { useAuth } from '@/lib/auth/useAuth';
import { toUserMessage } from '@/lib/api/errorMessage';
import { AuthScreen } from './AuthScreen';
import { signInSchema } from './auth.schemas';
import type { SignInValues } from './auth.schemas';

export function SignInPage() {
  const { status, signIn } = useAuth();
  const location = useLocation();
  // Where the guard was sending the user before it bounced them here (AC-9).
  const from = (location.state as { from?: string } | null)?.from ?? '/profile';
  const [submitError, setSubmitError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<SignInValues>({
    resolver: zodResolver(signInSchema),
    defaultValues: { email: '', password: '' },
  });

  // Already signed in (or just signed in): the status flip re-renders us here and
  // sends the user on to where they were headed. One redirect path, no race.
  if (status === 'authenticated') return <Navigate to={from} replace />;

  const onSubmit = async (values: SignInValues) => {
    setSubmitError(null);
    try {
      await signIn(values.email, values.password);
    } catch (error) {
      setSubmitError(toUserMessage(error, 'We could not sign you in. Please try again.'));
    }
  };

  return (
    <AuthScreen
      title="Sign in to Quiztin"
      subtitle="Welcome back. Pick up where you left off."
      error={submitError}
      footer={
        <>
          New to Quiztin?{' '}
          <Link to="/register" state={{ from }} className="text-text-link">
            Create an account
          </Link>
        </>
      }
    >
      <form
        onSubmit={(e) => void handleSubmit(onSubmit)(e)}
        noValidate
        className="flex flex-col gap-5"
      >
        <TextField
          label="Email"
          type="email"
          autoComplete="email"
          icon={EnvelopeSimple}
          required
          error={errors.email?.message}
          {...register('email')}
        />
        <TextField
          label="Password"
          type="password"
          autoComplete="current-password"
          icon={Lock}
          required
          error={errors.password?.message}
          {...register('password')}
        />
        <Button type="submit" size="lg" fullWidth loading={isSubmitting}>
          Sign in
        </Button>
      </form>
    </AuthScreen>
  );
}
