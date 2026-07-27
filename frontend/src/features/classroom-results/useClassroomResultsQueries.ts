import { useQuery } from '@tanstack/react-query';
import { qk } from '@/lib/api/queryKeys';
import {
  getClassroomResults,
  getQuizResults,
  getStudentAttempt,
  getStudentRollup,
} from './classroomResults.api';

/** The per-quiz summary for a classroom (spec 0010, AC-2). */
export function useClassroomResults(classroomId: string) {
  return useQuery({
    queryKey: qk.classroomResults(classroomId),
    queryFn: () => getClassroomResults(classroomId),
    enabled: classroomId !== '',
  });
}

/** One quiz's results, a page of students at a time (AC-3, AC-10). */
export function useQuizResults(quizId: string, page: number) {
  return useQuery({
    queryKey: qk.quizResults(quizId, page),
    queryFn: () => getQuizResults(quizId, page),
    enabled: quizId !== '',
  });
}

/** The per-student roll-up for a classroom, a page of students at a time (AC-5, AC-10). */
export function useStudentRollup(classroomId: string, page: number) {
  return useQuery({
    queryKey: qk.studentRollup(classroomId, page),
    queryFn: () => getStudentRollup(classroomId, page),
    enabled: classroomId !== '',
  });
}

/** One student's latest submitted attempt, for the drill-down (AC-6). */
export function useStudentAttempt(quizId: string, studentId: string) {
  return useQuery({
    queryKey: qk.studentAttempt(quizId, studentId),
    queryFn: () => getStudentAttempt(quizId, studentId),
    enabled: quizId !== '' && studentId !== '',
  });
}
