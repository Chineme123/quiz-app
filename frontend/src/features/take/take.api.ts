import { apiFetch } from '@/lib/api/client';
import { ApiError } from '@/lib/api/errors';
import { attemptResultSchema } from '@/features/results/results.schemas';
import type { AttemptResult } from '@/features/results/results.schemas';
import { attemptQuestionsSchema, availableQuizzesSchema, startAttemptSchema } from './take.schemas';
import type { AttemptQuestions, AvailableQuizzes } from './take.schemas';

/** The quizzes this student may take. Scoped to them by the server, so there is nothing to pass. */
export async function getAvailableQuizzes(): Promise<AvailableQuizzes> {
  return apiFetch('/api/quizzes/available', { schema: availableQuizzesSchema });
}

/** Begins an attempt. The server pins the deadline at this moment and returns the new id. */
export async function startQuiz(quizId: string): Promise<string> {
  const { attemptId } = await apiFetch(`/api/quizzes/${quizId}/start`, {
    method: 'POST',
    schema: startAttemptSchema,
  });
  return attemptId;
}

/**
 * The questions for one attempt, plus its deadline, the server's clock, and any answers
 * already saved. A 404 means the attempt does not exist or is not the caller's (the backend
 * answers the same either way so it never reveals another student's work, AC-5).
 */
export async function getAttemptQuestions(attemptId: string): Promise<AttemptQuestions | null> {
  try {
    return await apiFetch(`/api/attempts/${attemptId}/questions`, { schema: attemptQuestionsSchema });
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) return null;
    throw error;
  }
}

/**
 * Saves the whole answer set in one write (AC-6). Sends everything the screen holds rather
 * than a delta, so two saves in flight cannot interleave and drop an answer: the last one wins.
 * A 409 means this attempt takes no more writes (time is up, or it is already finished), which
 * the caller surfaces rather than retrying forever.
 */
export async function saveDraftAnswers(
  attemptId: string,
  answers: Record<string, string>,
): Promise<void> {
  await apiFetch(`/api/attempts/${attemptId}/answers`, {
    method: 'PUT',
    json: { answers },
  });
}

/**
 * Submits the attempt. Sends only an idempotency key: the answers already saved are what gets
 * graded (AC-11). Reuse the same commandId on a retry so a network hiccup cannot double grade.
 */
export async function submitAttempt(attemptId: string, commandId: string): Promise<AttemptResult> {
  return apiFetch(`/api/attempts/${attemptId}/submit`, {
    method: 'POST',
    json: { commandId },
    schema: attemptResultSchema,
  });
}
