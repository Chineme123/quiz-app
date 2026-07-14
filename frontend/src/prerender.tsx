import { renderToString } from 'react-dom/server';
import { createMemoryRouter } from 'react-router';
import { AppRoot } from './AppRoot';
import { routes } from './routes';
import { buildHeadTags } from './features/landing/seo';

/**
 * Build time prerender of the landing route (spec 0003, AC-11). Renders the exact
 * app tree the client hydrates, only with a memory router pinned to "/", so the
 * emitted HTML carries the hero copy and every section for a crawler that runs no
 * JavaScript. Returns the body markup plus the head tags; the prerender script
 * (scripts/prerender.mjs) injects both into the built index.html template.
 */
export function render(siteUrl: string): { html: string; head: string } {
  const router = createMemoryRouter(routes, { initialEntries: ['/'] });
  const html = renderToString(<AppRoot router={router} />);
  return { html, head: buildHeadTags(siteUrl) };
}
