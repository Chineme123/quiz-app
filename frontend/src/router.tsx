import { createBrowserRouter } from 'react-router';
import { AppShell } from '@/layout/AppShell';
import { RequireAuth } from '@/layout/RequireAuth';
import { NotFound } from '@/layout/NotFound';
import { LandingPage } from '@/features/landing/LandingPage';
import { SignInPage } from '@/features/auth/SignInPage';
import { RegisterPage } from '@/features/auth/RegisterPage';
import { ManageProfilePage } from '@/features/profile/ManageProfilePage';

// Data router used for routing only (nested routes, guards). Loaders and actions
// stay unused: TanStack Query owns all server state (spec 0001, foundation §7 #25).
//
// `/` is the public marketing landing page, shown to everyone (spec 0003). It sits
// outside RequireAuth; the old authenticated index redirect (bare `/` to `/profile`)
// was removed so the landing owns the root.
export const router = createBrowserRouter([
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
]);
