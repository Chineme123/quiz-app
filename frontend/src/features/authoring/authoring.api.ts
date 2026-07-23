import { apiFetch } from '@/lib/api/client';
import { ApiError } from '@/lib/api/errors';
import {
  authoredQuizSchema,
  quizSummariesSchema,
  type AuthoredQuiz,
  type QuestionType,
  type QuizSummary,
} from './authoring.schemas';

/**
 * The fields an add or edit question request sends. Every field is optional on the wire; only the
 * ones a given type needs are read, and the server's QuestionFactory is the single gate on whether
 * what arrived is a valid question of that type.
 */
export interface QuestionInput {
  questionType: QuestionType;
  prompt: string;
  points: number;
  options?: string[];
  correctOptionIndex?: number;
  correctAnswerBool?: boolean;
  correctAnswerText?: string;
}

/** Publish writes the availability window and the attempt limit, then makes the quiz live. */
export interface PublishInput {
  availableFrom: string | null;
  availableTo: string | null;
  maxAttempts: number;
}

/** The teacher's quizzes in one class. Null when the class is not theirs (404). */
export async function getClassroomQuizzes(classroomId: string): Promise<QuizSummary[] | null> {
  try {
    return await apiFetch(`/api/classrooms/${classroomId}/quizzes`, { schema: quizSummariesSchema });
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) return null;
    throw error;
  }
}

export function createQuiz(
  classroomId: string,
  title: string,
  durationMinutes: number,
): Promise<AuthoredQuiz> {
  return apiFetch(`/api/classrooms/${classroomId}/quizzes`, {
    method: 'POST',
    json: { title, durationMinutes },
    schema: authoredQuizSchema,
  });
}

/** The full quiz for editing. Null when it is not theirs (404), so existence never leaks. */
export async function getQuiz(quizId: string): Promise<AuthoredQuiz | null> {
  try {
    return await apiFetch(`/api/quizzes/${quizId}`, { schema: authoredQuizSchema });
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) return null;
    throw error;
  }
}

export function addQuestion(quizId: string, question: QuestionInput): Promise<AuthoredQuiz> {
  return apiFetch(`/api/quizzes/${quizId}/questions`, {
    method: 'POST',
    json: question,
    schema: authoredQuizSchema,
  });
}

export function editQuestion(
  quizId: string,
  questionId: string,
  question: QuestionInput,
): Promise<AuthoredQuiz> {
  return apiFetch(`/api/quizzes/${quizId}/questions/${questionId}`, {
    method: 'PATCH',
    json: question,
    schema: authoredQuizSchema,
  });
}

export async function deleteQuestion(quizId: string, questionId: string): Promise<void> {
  await apiFetch(`/api/quizzes/${quizId}/questions/${questionId}`, { method: 'DELETE' });
}

export function publishQuiz(quizId: string, input: PublishInput): Promise<AuthoredQuiz> {
  return apiFetch(`/api/quizzes/${quizId}/publish`, {
    method: 'POST',
    json: input,
    schema: authoredQuizSchema,
  });
}

export function unpublishQuiz(quizId: string): Promise<AuthoredQuiz> {
  return apiFetch(`/api/quizzes/${quizId}/unpublish`, {
    method: 'POST',
    schema: authoredQuizSchema,
  });
}
