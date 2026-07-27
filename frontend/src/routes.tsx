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
import { ClassQuizListPage } from '@/features/authoring/ClassQuizListPage';
import { QuizEditorPage } from '@/features/authoring/QuizEditorPage';
import { ClassroomResultsPage } from '@/features/classroom-results/ClassroomResultsPage';
import { QuizResultsPage } from '@/features/classroom-results/QuizResultsPage';
import { StudentRollupPage } from '@/features/classroom-results/StudentRollupPage';
import { StudentAttemptPage } from '@/features/classroom-results/StudentAttemptPage';

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
          // The teacher's authoring surface (spec 0009). Nested under the class because the
          // top level `quizzes` is already the student's take list.
          { path: 'classrooms/:classroomId/quizzes', element: <ClassQuizListPage /> },
          { path: 'quizzes/:quizId/edit', element: <QuizEditorPage /> },
          // Teacher classroom results (spec 0010): the summary, the per-student roll-up, one
          // quiz's results, and the drill-down into a student's attempt. Owner scoped; a student
          // reaching any of these gets the calm not-found state (the 404 reads as null).
          { path: 'classrooms/:classroomId/results', element: <ClassroomResultsPage /> },
          { path: 'classrooms/:classroomId/results/students', element: <StudentRollupPage /> },
          { path: 'quizzes/:quizId/results', element: <QuizResultsPage /> },
          { path: 'quizzes/:quizId/results/students/:studentId', element: <StudentAttemptPage /> },
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
