import { renderToString } from 'react-dom/server';
import { createMemoryRouter } from 'react-router';
import { AppRoot } from './AppRoot';
import { otherRoutes } from './routes';
import { LandingPage } from './features/landing/LandingPage';
import { buildHeadTags } from './features/landing/seo';

/**
 * Build time prerender of the landing route (spec 0003, AC-11). Renders the exact
 * app tree the client hydrates, only with a memory router pinned to "/", so the
 * emitted HTML carries the hero copy and every section for a crawler that runs no
 * JavaScript. Returns the body markup plus the head tags; the prerender script
 * (scripts/prerender.mjs) injects both into the built index.html template.
 */
export function render(siteUrl: string): { html: string; head: string } {
  // The landing is eager here (Node, bundle size does not matter), so renderToString
  // resolves it synchronously and the emitted tree matches what the client hydrates.
  const router = createMemoryRouter([{ path: '/', element: <LandingPage /> }, ...otherRoutes], {
    initialEntries: ['/'],
  });
  const html = renderToString(<AppRoot router={router} />);
  return { html, head: buildHeadTags(siteUrl) };
}
