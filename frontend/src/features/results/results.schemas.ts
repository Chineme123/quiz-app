import { z } from 'zod';

/**
 * The wire shape of GET /api/attempts/{id}/result (AttemptResultDto, camelCase),
 * validated at the boundary so a drifting backend fails loud here (spec 0005, AC-8).
 */
export const feedbackSourceSchema = z.enum(['Ai', 'Deterministic']);
export const feedbackStatusSchema = z.enum(['Pending', 'Ready']);

export const answerResultSchema = z.object({
  questionId: z.string(),
  questionText: z.string(),
  providedAnswer: z.string(),
  correctAnswer: z.string(),
  isCorrect: z.boolean(),
  pointsAwarded: z.number(),
  feedback: z.string().nullish(),
  feedbackSource: feedbackSourceSchema.nullish(),
});

export const attemptResultSchema = z.object({
  attemptId: z.string(),
  quizId: z.string(),
  totalScore: z.number().nullable(),
  feedbackStatus: feedbackStatusSchema,
  status: z.string(),
  answers: z.array(answerResultSchema),
});

export type AnswerResult = z.infer<typeof answerResultSchema>;
export type AttemptResult = z.infer<typeof attemptResultSchema>;
