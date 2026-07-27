import { describe, it, expect } from 'vitest';
import {
  classroomResultsSummarySchema,
  quizResultsSchema,
  studentRollupSchema,
} from './classroomResults.schemas';

// The page tests mock the api module, so the schemas never run there (the lesson from the guid
// incident, spec 0006). These parse representative payloads the server really sends, so a contract
// drift fails here rather than silently rendering wrong.

const GUID = '44444444-0000-0000-0000-000000000004';

describe('classroomResults schemas', () => {
  it('parses a classroom summary, including a quiz with no average yet', () => {
    const parsed = classroomResultsSummarySchema.parse({
      classroomId: GUID,
      classroomName: 'Biology 101',
      isArchived: false,
      studentCount: 3,
      quizzes: [
        { quizId: GUID, title: 'Cells', isPublished: true, totalPoints: 10, completionCount: 2, averageScore: 7.5, averagePercent: 75 },
        { quizId: GUID, title: 'Untaken', isPublished: true, totalPoints: 10, completionCount: 0, averageScore: null, averagePercent: null },
      ],
    });
    expect(parsed.quizzes[1]?.averageScore).toBeNull();
  });

  it('parses per-quiz results with a status enum and a nullable attempt id', () => {
    const parsed = quizResultsSchema.parse({
      quizId: GUID,
      classroomId: GUID,
      title: 'Cells',
      totalPoints: 10,
      studentCount: 2,
      completionCount: 1,
      averageScore: 8,
      averagePercent: 80,
      questions: [{ questionId: GUID, prompt: 'Q', points: 5, correctCount: 1, answeredCount: 1, fractionCorrect: 100 }],
      students: [
        { studentId: GUID, displayName: 'Alice', status: 'Completed', score: 8, percent: 80, attemptId: GUID },
        { studentId: GUID, displayName: 'bob@x.edu', status: 'NotTaken', score: null, percent: null, attemptId: null },
      ],
      total: 2,
      page: 1,
      pageSize: 20,
    });
    expect(parsed.students[1]?.status).toBe('NotTaken');
    expect(parsed.students[1]?.attemptId).toBeNull();
  });

  it('parses a roll-up with a null overall standing', () => {
    const parsed = studentRollupSchema.parse({
      classroomId: GUID,
      classroomName: 'Biology 101',
      quizzes: [{ quizId: GUID, title: 'Small', totalPoints: 10 }],
      students: [
        {
          studentId: GUID,
          displayName: 'Alice',
          scores: [{ quizId: GUID, status: 'InProgress', score: null, percent: null, attemptId: null }],
          overallStandingPercent: null,
        },
      ],
      total: 1,
      page: 1,
      pageSize: 20,
    });
    expect(parsed.students[0]?.overallStandingPercent).toBeNull();
  });

  it('rejects an unknown status and a non-guid id', () => {
    expect(() =>
      classroomResultsSummarySchema.parse({
        classroomId: 'not-a-guid',
        classroomName: 'x',
        isArchived: false,
        studentCount: 0,
        quizzes: [],
      }),
    ).toThrow();
  });
});
