import { createBrowserRouter, Navigate } from 'react-router';
import { AppShell } from '@/layout/AppShell';
import { RequireAuth } from '@/layout/RequireAuth';
import { NotFound } from '@/layout/NotFound';
import { SignInPage } from '@/features/auth/SignInPage';
import { RegisterPage } from '@/features/auth/RegisterPage';
import { ManageProfilePage } from '@/features/profile/ManageProfilePage';

// Data router used for routing only (nested routes, guards). Loaders and actions
// stay unused: TanStack Query owns all server state (spec 0001, foundation §7 #25).
export const router = createBrowserRouter([
  { path: '/sign-in', element: <SignInPage /> },
  { path: '/register', element: <RegisterPage /> },
  {
    element: <RequireAuth />,
    children: [
      {
        element: <AppShell />,
        children: [
          { index: true, element: <Navigate to="/profile" replace /> },
          { path: 'profile', element: <ManageProfilePage /> },
        ],
      },
    ],
  },
  { path: '*', element: <NotFound /> },
]);
