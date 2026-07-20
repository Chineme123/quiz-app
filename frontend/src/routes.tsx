import type { RouteObject } from 'react-router';
import { AppShell } from '@/layout/AppShell';
import { RequireAuth } from '@/layout/RequireAuth';
import { NotFound } from '@/layout/NotFound';
import { SignInPage } from '@/features/auth/SignInPage';
import { RegisterPage } from '@/features/auth/RegisterPage';
import { ManageProfilePage } from '@/features/profile/ManageProfilePage';
import { ResultsPage } from '@/features/results/ResultsPage';
import { QuizListPage } from '@/features/take/QuizListPage';
import { TakeQuizPage } from '@/features/take/TakeQuizPage';
import { DashboardPage } from '@/features/classrooms/DashboardPage';
import { JoinClassroomPage } from '@/features/classrooms/JoinClassroomPage';
import { ClassroomDetailPage } from '@/features/classrooms/ClassroomDetailPage';

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
          { path: 'dashboard', element: <DashboardPage /> },
          // Inside RequireAuth on purpose: a join link opened signed out routes through
          // /sign-in and resumes here afterwards, so one link works in a single pass (AC-4).
          { path: 'join/:code', element: <JoinClassroomPage /> },
          { path: 'classrooms/:classroomId', element: <ClassroomDetailPage /> },
          { path: 'profile', element: <ManageProfilePage /> },
          { path: 'quizzes', element: <QuizListPage /> },
          { path: 'attempts/:attemptId/take', element: <TakeQuizPage /> },
          { path: 'results/:attemptId', element: <ResultsPage /> },
        ],
      },
    ],
  },
  { path: '*', element: <NotFound /> },
];
