import type { RouteObject } from 'react-router';
import { AppShell } from '@/layout/AppShell';
import { RequireAuth } from '@/layout/RequireAuth';
import { NotFound } from '@/layout/NotFound';
import { LandingPage } from '@/features/landing/LandingPage';
import { SignInPage } from '@/features/auth/SignInPage';
import { RegisterPage } from '@/features/auth/RegisterPage';
import { ManageProfilePage } from '@/features/profile/ManageProfilePage';

// The route table, shared by the client browser router (router.tsx) and the build
// time prerender's memory router (prerender.tsx). This module creates NO router, so
// importing it never touches the browser History API, which is what lets the
// prerender load the same routes in Node (spec 0003, AC-11).
//
// `/` is the public marketing landing page, shown to everyone (spec 0003). It sits
// outside RequireAuth; the old authenticated index redirect (bare `/` to `/profile`)
// was removed so the landing owns the root.
export const routes: RouteObject[] = [
  { path: '/', element: <LandingPage /> },
  { path: '/sign-in', element: <SignInPage /> },
  { path: '/register', element: <RegisterPage /> },
  {
    element: <RequireAuth />,
    children: [
      {
        element: <AppShell />,
        children: [{ path: 'profile', element: <ManageProfilePage /> }],
      },
    ],
  },
  { path: '*', element: <NotFound /> },
];
