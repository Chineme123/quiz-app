import { z } from 'zod';
import { guid } from '@/lib/api/schemas';

/**
 * The wire shape of GET /api/results/mine (MyResultsDto, camelCase), validated at the boundary so a
 * drifting backend fails loud here rather than rendering wrong numbers (spec 0011). The page tests
 * mock the api, so zod never runs there; this is what the schema test guards (the lesson from 0006).
 */

export const myResultsQuizSchema = z.object({
  quizId: guid,
  title: z.string(),
  totalPoints: z.number(),
  score: z.number().nullable(),
  percent: z.number().nullable(),
  attemptId: guid,
  submittedAt: z.string(),
});

export const myResultsClassroomSchema = z.object({
  classroomId: guid,
  classroomName: z.string(),
  isArchived: z.boolean(),
  standingPercent: z.number().nullable(),
  quizzes: z.array(myResultsQuizSchema),
});

export const myResultsSchema = z.object({
  classrooms: z.array(myResultsClassroomSchema),
});

export type MyResultsQuiz = z.infer<typeof myResultsQuizSchema>;
export type MyResultsClassroom = z.infer<typeof myResultsClassroomSchema>;
export type MyResults = z.infer<typeof myResultsSchema>;
