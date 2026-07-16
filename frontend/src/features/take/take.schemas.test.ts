import { describe, it, expect } from 'vitest';
import { availableQuizzesSchema, attemptQuestionsSchema } from './take.schemas';

/**
 * The boundary contract itself, parsed against the ids the server really sends.
 *
 * These exist because the page tests mock the api module, so the schema never runs in them: a
 * schema that rejected every seeded id shipped with twelve green tests and took the whole quiz
 * list down. Anything asserting on the WIRE SHAPE belongs here, unmocked, not there.
 */

// A real seeded id from the dev database. It is a valid .NET Guid but NOT an RFC 4122 v4 UUID
// (its version nibble is 0), which is exactly the case z.uuid() rejected.
const SEEDED_QUIZ_ID = '44444444-0000-0000-0000-000000000004';
// A Guid.NewGuid() style id, which is v4. Both must parse.
const GENERATED_ID = 'b7f3c1a2-9d4e-4f6a-8b2c-1e5d7a9f0c3b';

describe('take schemas accept the ids the server actually sends', () => {
  it('parses a seeded, non v4 quiz id', () => {
    const parsed = availableQuizzesSchema.safeParse({
      total: 1,
      page: 1,
      pageSize: 20,
      items: [
        {
          quizId: SEEDED_QUIZ_ID,
          title: 'Seeded quiz',
          durationMinutes: 15,
          questionCount: 3,
          state: 'NotStarted',
          attemptId: null,
        },
      ],
    });

    expect(parsed.success).toBe(true);
  });

  it('parses a generated v4 id too', () => {
    const parsed = availableQuizzesSchema.safeParse({
      total: 1,
      page: 1,
      pageSize: 20,
      items: [
        {
          quizId: GENERATED_ID,
          title: 'Started quiz',
          durationMinutes: 15,
          questionCount: 3,
          state: 'InProgress',
          attemptId: GENERATED_ID,
        },
      ],
    });

    expect(parsed.success).toBe(true);
  });

  it('parses an attempt questions payload with seeded question ids', () => {
    const parsed = attemptQuestionsSchema.safeParse({
      attemptId: GENERATED_ID,
      quizTitle: 'Seeded quiz',
      status: 'InProgress',
      expiresAt: '2026-07-16T12:15:00Z',
      serverNow: '2026-07-16T12:00:00Z',
      draftAnswers: { [SEEDED_QUIZ_ID]: '1' },
      questions: [
        {
          id: SEEDED_QUIZ_ID,
          questionType: 'MultipleChoiceQuestion',
          prompt: '2 + 2?',
          points: 10,
          options: ['3', '4', '5'],
        },
      ],
    });

    expect(parsed.success).toBe(true);
  });

  it('still rejects something that is not a guid at all', () => {
    const parsed = availableQuizzesSchema.safeParse({
      total: 1,
      page: 1,
      pageSize: 20,
      items: [
        {
          quizId: 'not-a-guid',
          title: 'Bad',
          durationMinutes: 15,
          questionCount: 3,
          state: 'NotStarted',
          attemptId: null,
        },
      ],
    });

    expect(parsed.success).toBe(false);
  });
});
