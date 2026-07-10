import type { ReactNode } from 'react';
import { Link } from 'react-router';
import { Card } from '@/components/ui';

interface AuthScreenProps {
  title: string;
  subtitle: string;
  /** A submit-time error, shown assertively above the form. */
  error?: string | null;
  children: ReactNode;
  /** The "switch to the other flow" line below the card. */
  footer: ReactNode;
}

/** Shared frame for the two public auth screens: brand, card, error banner, footer. */
export function AuthScreen({ title, subtitle, error, children, footer }: AuthScreenProps) {
  return (
    <div className="grid min-h-screen place-items-center bg-bg p-6">
      <div className="w-full max-w-md">
        <div className="mb-6 text-center">
          <Link to="/sign-in" className="font-display text-2xl font-semibold text-text-strong no-underline">
            Quiztin<span className="text-accent">.</span>
          </Link>
        </div>

        <Card variant="raised" padding="lg">
          <h1 className="font-display text-2xl text-text-strong">{title}</h1>
          <p className="mt-2 text-text-body">{subtitle}</p>

          {error && (
            <p
              role="alert"
              className="mt-5 rounded-field border border-danger bg-danger-soft px-4 py-3 text-danger-text"
            >
              {error}
            </p>
          )}

          <div className="mt-6">{children}</div>
        </Card>

        <p className="mt-6 text-center text-text-muted">{footer}</p>
      </div>
    </div>
  );
}
