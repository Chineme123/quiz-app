/**
 * Landing page SEO (spec 0003, AC-11). Pure strings only, no browser globals, so
 * this bundles cleanly into the build time prerender. The head tags below are
 * injected into the prerendered `/` document; the neutral SPA bootstrap keeps its
 * own generic title, so no other route inherits this marketing metadata (AC-14).
 */

/** The canonical origin. Overridable at build time via SITE_URL; see the spec follow up. */
export const DEFAULT_SITE_URL = 'https://quiztin.up.railway.app';

export const SEO = {
  title: 'Quiztin: calmer classroom quizzes with instant feedback',
  description:
    'Quiztin is a calmer way to run classroom quizzes. Teachers build a quiz once and Quiztin scores it the moment a student submits, with warm AI feedback on every question. Free for classrooms.',
  /** Relative path of the share image; resolved against the site origin below. Adding the actual image is a spec follow up. */
  ogImagePath: '/og-image.png',
} as const;

/** Escape the few characters that would break an attribute value or a script block. */
function escapeHtml(value: string): string {
  return value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

/**
 * Build the full head markup for the prerendered landing page: title, description,
 * Open Graph and Twitter cards, a canonical link, and product JSON-LD. The origin
 * comes in as an argument so the prerender script owns where it reads it from.
 */
export function buildHeadTags(siteUrl: string): string {
  const origin = siteUrl.replace(/\/$/, '');
  const canonical = `${origin}/`;
  const ogImage = `${origin}${SEO.ogImagePath}`;
  const title = escapeHtml(SEO.title);
  const description = escapeHtml(SEO.description);

  const jsonLd = {
    '@context': 'https://schema.org',
    '@type': 'SoftwareApplication',
    name: 'Quiztin',
    applicationCategory: 'EducationalApplication',
    operatingSystem: 'Web',
    description: SEO.description,
    url: canonical,
    offers: {
      '@type': 'Offer',
      price: '0',
      priceCurrency: 'USD',
    },
  };
  // Guard the closing tag so the JSON can never break out of the script element.
  const jsonLdText = JSON.stringify(jsonLd).replace(/</g, '\\u003c');

  return [
    `<title>${title}</title>`,
    `<meta name="description" content="${description}" />`,
    `<link rel="canonical" href="${canonical}" />`,
    `<meta property="og:type" content="website" />`,
    `<meta property="og:site_name" content="Quiztin" />`,
    `<meta property="og:title" content="${title}" />`,
    `<meta property="og:description" content="${description}" />`,
    `<meta property="og:url" content="${canonical}" />`,
    `<meta property="og:image" content="${ogImage}" />`,
    `<meta name="twitter:card" content="summary_large_image" />`,
    `<meta name="twitter:title" content="${title}" />`,
    `<meta name="twitter:description" content="${description}" />`,
    `<meta name="twitter:image" content="${ogImage}" />`,
    `<script type="application/ld+json">${jsonLdText}</script>`,
  ].join('\n    ');
}
