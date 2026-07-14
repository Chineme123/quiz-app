import { Link } from 'react-router';
import { useAuth } from '@/lib/auth/useAuth';
import { NAV_LINKS } from '../content';

/**
 * The public marketing nav. It adapts to auth state (AC-5): a signed out visitor
 * gets "Sign in" and "Get started"; a signed in visitor gets a link into the app.
 * The CTAs are anchors, not the Button component, because they navigate; they
 * borrow the button look through the shared qz-btn classes.
 */
export function LandingNav() {
  const { status } = useAuth();
  const signedIn = status === 'authenticated';

  return (
    <header className="qz-nav">
      <div className="qz-nav__inner">
        <Link to="/" className="qz-wordmark" aria-label="Quiztin home">
          Quiztin<span className="qz-wordmark__dot">.</span>
        </Link>

        <nav aria-label="Primary" className="qz-nav__links">
          {NAV_LINKS.map((link) => (
            <a key={link.href} href={link.href} className="qz-nav__link">
              {link.label}
            </a>
          ))}
        </nav>

        <div className="qz-nav__actions">
          {signedIn ? (
            <Link to="/profile" className="qz-btn qz-btn--primary qz-btn--md">
              Go to dashboard
            </Link>
          ) : (
            <>
              <Link to="/sign-in" className="qz-btn qz-btn--ghost qz-btn--md">
                Sign in
              </Link>
              <Link to="/register" className="qz-btn qz-btn--accent qz-btn--md">
                Get started
              </Link>
            </>
          )}
        </div>
      </div>
    </header>
  );
}
