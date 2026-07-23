import { z } from 'zod';
import { guid } from '@/lib/api/schemas';

/**
 * The question types the authoring API accepts and returns. The server normalises its storage
 * discriminator to these same short names, so a request and a response speak one vocabulary
 * (spec 0009).
 */
export const questionTypeSchema = z.enum(['MultipleChoice', 'TrueFalse', 'ShortAnswer']);
export type QuestionType = z.infer<typeof questionTypeSchema>;

/**
 * One question as the owner's editor sees it. Unlike the take path, this carries the correct
 * answer: the teacher cannot edit a question without it. Owner scoped, never sent to a student.
 */
export const authoredQuestionSchema = z.object({
  id: guid,
  questionType: questionTypeSchema,
  prompt: z.string(),
  points: z.number(),
  options: z.array(z.string()).nullable(),
  correctOptionIndex: z.number().nullable(),
  correctAnswerBool: z.boolean().nullable(),
  correctAnswerText: z.string().nullable(),
});
export type AuthoredQuestion = z.infer<typeof authoredQuestionSchema>;

/** The full quiz for editing: questions, settings, publish state, and the locked flag. */
export const authoredQuizSchema = z.object({
  id: guid,
  title: z.string(),
  durationMinutes: z.number(),
  classroomId: guid,
  teacherId: guid,
  isPublished: z.boolean(),
  availableFrom: z.string().nullable(),
  availableTo: z.string().nullable(),
  maxAttempts: z.number(),
  /** True once a student has an attempt: the question set is fixed from then on. */
  isLocked: z.boolean(),
  questions: z.array(authoredQuestionSchema),
});
export type AuthoredQuiz = z.infer<typeof authoredQuizSchema>;

/** One row of the teacher's per class quiz list. */
export const quizSummarySchema = z.object({
  id: guid,
  title: z.string(),
  isPublished: z.boolean(),
  questionCount: z.number(),
  attemptCount: z.number(),
});
export type QuizSummary = z.infer<typeof quizSummarySchema>;

export const quizSummariesSchema = z.array(quizSummarySchema);
