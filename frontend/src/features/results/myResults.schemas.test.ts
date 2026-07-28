import { describe, it, expect } from 'vitest';
import { myResultsSchema } from './myResults.schemas';

// The page test mocks the api module, so this schema never runs there (the lesson from the guid
// incident, spec 0006). These parse representative payloads the server really sends, so a contract
// drift fails here rather than silently rendering wrong.

const GUID = '44444444-0000-0000-0000-000000000004';

describe('myResults schemas', () => {
  it('parses grouped results, including a null percent and a null standing', () => {
    const parsed = myResultsSchema.parse({
      classrooms: [
        {
          classroomId: GUID,
          classroomName: 'Networking',
          isArchived: false,
          standingPercent: 66.7,
          quizzes: [
            { quizId: GUID, title: 'Basics', totalPoints: 3, score: 2, percent: 66.7, attemptId: GUID, submittedAt: '2026-07-27T10:00:00Z' },
            // A quiz with no points: percent is null (the divide is guarded), score still shows.
            { quizId: GUID, title: 'No points', totalPoints: 0, score: 0, percent: null, attemptId: GUID, submittedAt: '2026-07-27T10:00:00Z' },
          ],
        },
        // A class the student is in but has finished nothing: no standing, no rows.
        { classroomId: GUID, classroomName: 'Empty', isArchived: true, standingPercent: null, quizzes: [] },
      ],
    });

    expect(parsed.classrooms).toHaveLength(2);
    expect(parsed.classrooms[0]?.quizzes[1]?.percent).toBeNull();
    expect(parsed.classrooms[1]?.standingPercent).toBeNull();
    expect(parsed.classrooms[1]?.quizzes).toHaveLength(0);
  });

  it('parses an empty result (the student has finished nothing anywhere)', () => {
    const parsed = myResultsSchema.parse({ classrooms: [] });
    expect(parsed.classrooms).toHaveLength(0);
  });

  it('rejects a non-guid id', () => {
    expect(() =>
      myResultsSchema.parse({
        classrooms: [
          { classroomId: 'not-a-guid', classroomName: 'x', isArchived: false, standingPercent: null, quizzes: [] },
        ],
      }),
    ).toThrow();
  });
});
