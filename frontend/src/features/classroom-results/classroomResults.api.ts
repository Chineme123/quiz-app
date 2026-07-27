import { apiFetch } from '@/lib/api/client';
import { ApiError } from '@/lib/api/errors';
import { attemptResultSchema } from '@/features/results/results.schemas';
import type { AttemptResult } from '@/features/results/results.schemas';
import {
  classroomResultsSummarySchema,
  quizResultsSchema,
  studentRollupSchema,
} from './classroomResults.schemas';
import type { ClassroomResultsSummary, QuizResults, StudentRollup } from './classroomResults.schemas';

/**
 * Teacher classroom results (spec 0010). Every read is owner scoped by the server, so a 404 means
 * "no such class/quiz" or "not yours" alike — the same answer either way, which the screens render
 * as the calm not-found state, never a distinct "forbidden" (AC-1, AC-12). Every other failure
 * propagates to the error state.
 */

export async function getClassroomResults(classroomId: string): Promise<ClassroomResultsSummary | null> {
  try {
    return await apiFetch(`/api/classrooms/${classroomId}/results`, {
      schema: classroomResultsSummarySchema,
    });
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) return null;
    throw error;
  }
}

export async function getQuizResults(
  quizId: string,
  page: number,
  pageSize = 20,
): Promise<QuizResults | null> {
  try {
    return await apiFetch(`/api/quizzes/${quizId}/results?page=${page}&pageSize=${pageSize}`, {
      schema: quizResultsSchema,
    });
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) return null;
    throw error;
  }
}

export async function getStudentRollup(
  classroomId: string,
  page: number,
  pageSize = 20,
): Promise<StudentRollup | null> {
  try {
    return await apiFetch(`/api/classrooms/${classroomId}/results/students?page=${page}&pageSize=${pageSize}`, {
      schema: studentRollupSchema,
    });
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) return null;
    throw error;
  }
}

/** One student's latest submitted attempt on a quiz, for the teacher's drill-down (AC-6). Reuses
 *  the student attempt-result shape, since it is the same per-question breakdown. */
export async function getStudentAttempt(
  quizId: string,
  studentId: string,
): Promise<AttemptResult | null> {
  try {
    return await apiFetch(`/api/quizzes/${quizId}/results/students/${studentId}`, {
      schema: attemptResultSchema,
    });
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) return null;
    throw error;
  }
}
