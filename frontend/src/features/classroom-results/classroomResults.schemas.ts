import { z } from 'zod';
import { guid } from '@/lib/api/schemas';

/**
 * The wire shapes for teacher classroom results (spec 0010), camelCase, validated at the boundary
 * so a drifting backend fails loud here rather than rendering wrong numbers. The drill-down reuses
 * the student attempt-result shape from `features/results`, so it is not redefined here.
 */

/** A per-(student, quiz) cell state, shared by the per-quiz list and the roll-up. */
export const resultStatusSchema = z.enum(['Completed', 'InProgress', 'NotTaken']);

// --- Classroom summary: GET /api/classrooms/{id}/results ---------------------------------------

export const quizResultSummarySchema = z.object({
  quizId: guid,
  title: z.string(),
  isPublished: z.boolean(),
  totalPoints: z.number(),
  completionCount: z.number(),
  averageScore: z.number().nullable(),
  averagePercent: z.number().nullable(),
});

export const classroomResultsSummarySchema = z.object({
  classroomId: guid,
  classroomName: z.string(),
  isArchived: z.boolean(),
  studentCount: z.number(),
  quizzes: z.array(quizResultSummarySchema),
});

// --- Per-quiz results: GET /api/quizzes/{id}/results --------------------------------------------

export const questionDifficultySchema = z.object({
  questionId: guid,
  prompt: z.string(),
  points: z.number(),
  correctCount: z.number(),
  answeredCount: z.number(),
  fractionCorrect: z.number().nullable(),
});

export const quizStudentResultSchema = z.object({
  studentId: guid,
  displayName: z.string(),
  status: resultStatusSchema,
  score: z.number().nullable(),
  percent: z.number().nullable(),
  attemptId: guid.nullable(),
});

export const quizResultsSchema = z.object({
  quizId: guid,
  classroomId: guid,
  title: z.string(),
  totalPoints: z.number(),
  studentCount: z.number(),
  completionCount: z.number(),
  averageScore: z.number().nullable(),
  averagePercent: z.number().nullable(),
  questions: z.array(questionDifficultySchema),
  students: z.array(quizStudentResultSchema),
  total: z.number(),
  page: z.number(),
  pageSize: z.number(),
});

// --- Per-student roll-up: GET /api/classrooms/{id}/results/students -----------------------------

export const rollupQuizColumnSchema = z.object({
  quizId: guid,
  title: z.string(),
  totalPoints: z.number(),
});

export const studentQuizScoreSchema = z.object({
  quizId: guid,
  status: resultStatusSchema,
  score: z.number().nullable(),
  percent: z.number().nullable(),
  attemptId: guid.nullable(),
});

export const studentRollupRowSchema = z.object({
  studentId: guid,
  displayName: z.string(),
  scores: z.array(studentQuizScoreSchema),
  overallStandingPercent: z.number().nullable(),
});

export const studentRollupSchema = z.object({
  classroomId: guid,
  classroomName: z.string(),
  quizzes: z.array(rollupQuizColumnSchema),
  students: z.array(studentRollupRowSchema),
  total: z.number(),
  page: z.number(),
  pageSize: z.number(),
});

export type ResultStatus = z.infer<typeof resultStatusSchema>;
export type QuizResultSummary = z.infer<typeof quizResultSummarySchema>;
export type ClassroomResultsSummary = z.infer<typeof classroomResultsSummarySchema>;
export type QuestionDifficulty = z.infer<typeof questionDifficultySchema>;
export type QuizStudentResult = z.infer<typeof quizStudentResultSchema>;
export type QuizResults = z.infer<typeof quizResultsSchema>;
export type RollupQuizColumn = z.infer<typeof rollupQuizColumnSchema>;
export type StudentQuizScore = z.infer<typeof studentQuizScoreSchema>;
export type StudentRollupRow = z.infer<typeof studentRollupRowSchema>;
export type StudentRollup = z.infer<typeof studentRollupSchema>;
