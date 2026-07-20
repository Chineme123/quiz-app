import { useAuth } from '@/lib/auth/useAuth';
import { TeacherDashboardPage } from './TeacherDashboardPage';
import { StudentDashboardPage } from './StudentDashboardPage';

/**
 * The signed in home (spec 0008, AC-2 and AC-5). One route, two homes: a teacher runs classes,
 * a student joins them, and neither should have to find their half in a menu.
 *
 * The branch lives here rather than in the sign in redirect so any route into `/dashboard`
 * (a bookmark, a reload, the wordmark) lands the right person in the right place.
 */
export function DashboardPage() {
  const { user } = useAuth();

  return user?.role === 'Teacher' ? <TeacherDashboardPage /> : <StudentDashboardPage />;
}
