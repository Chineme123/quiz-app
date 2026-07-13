import { LandingNav } from './sections/LandingNav';
import { Hero } from './sections/Hero';
import { HowItWorks } from './sections/HowItWorks';
import { ValueSplit } from './sections/ValueSplit';
import { AISpotlight } from './sections/AISpotlight';
import { Faq } from './sections/Faq';
import { CtaBand } from './sections/CtaBand';
import { LandingFooter } from './sections/LandingFooter';
import './landing.css';

/**
 * The public marketing landing page (spec 0003), rendered at `/` for everyone.
 * Composed section by section; more sections (how it works, value split, AI
 * feedback spotlight, FAQ, free line, closing call to action) land in later slices.
 */
export function LandingPage() {
  return (
    <div className="qz-landing">
      <a href="#main-content" className="qz-skip">
        Skip to content
      </a>
      <LandingNav />
      <main id="main-content">
        <Hero />
        <HowItWorks />
        <ValueSplit />
        <AISpotlight />
        <Faq />
        <CtaBand />
      </main>
      <LandingFooter />
    </div>
  );
}
