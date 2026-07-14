import { createBrowserRouter } from 'react-router';
import { createRoot, hydrateRoot } from 'react-dom/client';
import { AppRoot } from './AppRoot';
import { otherRoutes } from './routes';
import './styles/tailwind.css';

// The landing page is always a dynamic import, so it is bundled as its own chunk,
// not part of this entry (spec 0003, AC-13). Signing straight into the app never
// downloads the landing or framer-motion; the chunk loads only when "/" is shown.
async function bootstrap() {
  const root = document.getElementById('root');
  if (!root) throw new Error('Root element #root not found.');

  if (root.firstElementChild) {
    // The prerendered "/" arrives with markup in #root. Load the landing chunk
    // first, then hydrate with an eager element so the tree matches the prerender
    // exactly and hydration is clean, no flash, no repaint (AC-11/AC-14).
    const { LandingPage } = await import('@/features/landing/LandingPage');
    const router = createBrowserRouter([{ path: '/', element: <LandingPage /> }, ...otherRoutes]);
    hydrateRoot(root, <AppRoot router={router} />);
  } else {
    // Any other route ships the neutral bootstrap with an empty #root. Here the
    // landing is lazy, so its chunk only loads if the user navigates to "/".
    const router = createBrowserRouter([
      {
        path: '/',
        lazy: async () => ({ Component: (await import('@/features/landing/LandingPage')).LandingPage }),
      },
      ...otherRoutes,
    ]);
    createRoot(root).render(<AppRoot router={router} />);
  }
}

void bootstrap();
