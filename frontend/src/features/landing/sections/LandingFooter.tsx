import { Link } from 'react-router';
import { FOOTER } from '../content';

/** The landing footer: brand, link columns, and the honest persona disclosure (AC-15). */
export function LandingFooter() {
  return (
    <footer className="qz-footer">
      <div className="qz-container qz-footer__grid">
        <div>
          <Link to="/" className="qz-footer__brand-mark" aria-label="Quiztin home">
            Quiztin<span className="qz-wordmark__dot">.</span>
          </Link>
          <p className="qz-footer__tagline">{FOOTER.tagline}</p>
        </div>

        <div className="qz-footer__cols">
          {FOOTER.columns.map((col) => (
            <div key={col.title}>
              <h2 className="qz-footer__col-title">{col.title}</h2>
              <nav aria-label={col.title}>
                {col.links.map((link) =>
                  link.href.startsWith('#') ? (
                    <a key={link.label} href={link.href} className="qz-footer__link">
                      {link.label}
                    </a>
                  ) : (
                    <Link key={link.label} to={link.href} className="qz-footer__link">
                      {link.label}
                    </Link>
                  ),
                )}
              </nav>
            </div>
          ))}
        </div>
      </div>

      <div className="qz-container qz-footer__fine">
        <p>{FOOTER.personaDisclosure}</p>
        <p>Quiztin is a student project built as coursework. © 2026 Quiztin.</p>
      </div>
    </footer>
  );
}
