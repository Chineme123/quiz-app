import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link, Navigate, useLocation, useSearchParams } from 'react-router';
import { EnvelopeSimple, Lock } from '@phosphor-icons/react';
import { Button, Select, TextField } from '@/components/ui';
import { useAuth } from '@/lib/auth/useAuth';
import { toUserMessage } from '@/lib/api/errorMessage';
import { roleSchema } from '@/lib/auth/session';
import { AuthScreen } from './AuthScreen';
import { registerSchema } from './auth.schemas';
import type { RegisterValues } from './auth.schemas';

const ROLE_OPTIONS = roleSchema.options.map((role) => ({ value: role, label: role }));

export function RegisterPage() {
  const { status, register: registerAccount } = useAuth();
  const location = useLocation();
  const [searchParams] = useSearchParams();
  // Same as sign in: land on the role aware dashboard unless a guard was mid redirect, so a
  // brand new teacher arrives at "create your first class" (spec 0008).
  const from = (location.state as { from?: string } | null)?.from ?? '/dashboard';
  const [submitError, setSubmitError] = useState<string | null>(null);

  // A landing page CTA may hint the role via ?role=Student|Teacher; preselect it when valid.
  const roleHint = roleSchema.safeParse(searchParams.get('role'));

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<RegisterValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: { email: '', password: '', ...(roleHint.success ? { role: roleHint.data } : {}) },
  });

  if (status === 'authenticated') return <Navigate to={from} replace />;

  const onSubmit = async (values: RegisterValues) => {
    setSubmitError(null);
    try {
      await registerAccount(values.email, values.password, values.role);
    } catch (error) {
      setSubmitError(toUserMessage(error, 'We could not create your account. Please try again.'));
    }
  };

  return (
    <AuthScreen
      title="Create your account"
      subtitle="Join Quiztin as a student or a teacher."
      error={submitError}
      footer={
        <>
          Already have an account?{' '}
          <Link to="/sign-in" state={{ from }} className="text-text-link">
            Sign in
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
          autoComplete="new-password"
          icon={Lock}
          required
          hint="At least 8 characters."
          error={errors.password?.message}
          {...register('password')}
        />
        <Select
          label="Role"
          placeholder="Choose your role"
          options={ROLE_OPTIONS}
          required
          error={errors.role?.message}
          {...register('role')}
        />
        <Button type="submit" size="lg" fullWidth loading={isSubmitting}>
          Create account
        </Button>
      </form>
    </AuthScreen>
  );
}
