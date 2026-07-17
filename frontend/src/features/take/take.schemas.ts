import { z } from 'zod';
import { guid } from '@/lib/api/schemas';

/**
 * The wire contract for taking a quiz (spec 0006). Validated at the boundary, so a server
 * change shows up here as a clear ContractError rather than as undefined halfway down a render.
 *
 * Enum-ish values are exact PascalCase because that is what the API sends: the question type is
 * the persisted discriminator, and the list state comes from the attempt's state name.
 */

export const availableQuizSchema = z.object({
  quizId: guid,
  title: z.string(),
  durationMinutes: z.number().int(),
  questionCount: z.number().int(),
  state: z.enum(['NotStarted', 'InProgress', 'Graded']),
  // Present for a quiz already started or finished: the attempt to resume, or to view.
  attemptId: guid.nullable(),
});

export const availableQuizzesSchema = z.object({
  items: z.array(availableQuizSchema),
  total: z.number().int(),
  page: z.number().int(),
  pageSize: z.number().int(),
});

export const attemptQuestionSchema = z.object({
  id: guid,
  questionType: z.enum(['MultipleChoiceQuestion', 'TrueFalseQuestion', 'ShortAnswerQuestion']),
  prompt: z.string(),
  points: z.number().int(),
  // Only a multiple choice question carries options.
  options: z.array(z.string()).nullable(),
});

export const attemptQuestionsSchema = z.object({
  attemptId: guid,
  quizTitle: z.string(),
  status: z.string(),
  // The deadline, pinned when the attempt started, and the server's clock at this read. The
  // countdown runs on the gap between them, never on the device clock (AC-10).
  expiresAt: z.string(),
  serverNow: z.string(),
  // questionId -> the answer already saved, so resuming restores the student's work (AC-9).
  draftAnswers: z.record(z.string(), z.string()),
  questions: z.array(attemptQuestionSchema),
});

export const startAttemptSchema = z.object({
  attemptId: guid,
});

export type AvailableQuiz = z.infer<typeof availableQuizSchema>;
export type AvailableQuizzes = z.infer<typeof availableQuizzesSchema>;
export type AttemptQuestion = z.infer<typeof attemptQuestionSchema>;
export type AttemptQuestions = z.infer<typeof attemptQuestionsSchema>;
