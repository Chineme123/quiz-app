import { StrictMode } from 'react';
import { RouterProvider } from 'react-router';
import type { createBrowserRouter } from 'react-router';
import { Providers } from './app/Providers';

type AppRouter = ReturnType<typeof createBrowserRouter>;

/**
 * The single app tree, shared by the client entry (main.tsx) and the build time
 * prerender (prerender.tsx). Only the router differs: a browser router in the app,
 * a memory router in the prerender. Keeping every wrapper above the routes identical
 * keeps useId and the whole render tree aligned, so the prerendered `/` hydrates
 * cleanly instead of repainting (spec 0003, AC-11/AC-14).
 */
export function AppRoot({ router }: { router: AppRouter }) {
  return (
    <StrictMode>
      <Providers>
        <RouterProvider router={router} />
      </Providers>
    </StrictMode>
  );
}
