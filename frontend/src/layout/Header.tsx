import { Link } from 'react-router';
import { ProfileMenu } from './ProfileMenu';

export function Header() {
  return (
    <header className="border-b border-border bg-surface-card">
      <div className="mx-auto flex max-w-content items-center justify-between px-6 py-4">
        <Link to="/dashboard" className="font-display text-2xl font-semibold text-text-strong no-underline">
          Quiztin<span className="text-accent">.</span>
        </Link>
        <ProfileMenu />
      </div>
    </header>
  );
}
