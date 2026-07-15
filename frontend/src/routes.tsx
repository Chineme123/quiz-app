import type { RouteObject } from 'react-router';
import { AppShell } from '@/layout/AppShell';
import { RequireAuth } from '@/layout/RequireAuth';
import { NotFound } from '@/layout/NotFound';
import { SignInPage } from '@/features/auth/SignInPage';
import { RegisterPage } from '@/features/auth/RegisterPage';
import { ManageProfilePage } from '@/features/profile/ManageProfilePage';
import { ResultsPage } from '@/features/results/ResultsPage';

// Every route EXCEPT the public landing at "/". The landing is added separately, in
// main.tsx and prerender.tsx, so it can be code split (spec 0003, AC-13): the client
// loads it as its own chunk (eagerly on the prerendered "/", lazily on any other
// route), and this module never imports it. That keeps the landing and framer-motion
// out of the authenticated app's entry chunk, so signing in does not download them.
export const otherRoutes: RouteObject[] = [
  { path: '/sign-in', element: <SignInPage /> },
  { path: '/register', element: <RegisterPage /> },
  {
    element: <RequireAuth />,
    children: [
      {
        element: <AppShell />,
        children: [
          { path: 'profile', element: <ManageProfilePage /> },
          { path: 'results/:attemptId', element: <ResultsPage /> },
        ],
      },
    ],
  },
  { path: '*', element: <NotFound /> },
];
