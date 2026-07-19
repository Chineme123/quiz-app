import { describe, it, expect } from 'vitest';
import {
  enrolledClassroomsSchema,
  joinPreviewSchema,
  ownedClassroomsSchema,
} from './classrooms.schemas';

/**
 * The wire contract, parsed for real.
 *
 * These exist because the page tests mock the api module, so the schemas never run there: a
 * drifting backend, or an id shape the schema is too strict for, would sail past every green
 * page test. That is exactly how the quiz list went down once (spec 0006).
 */
describe('classroom wire schemas', () => {
  // The seeded classroom id the server really sends. It is a valid .NET Guid but not a v4 UUID,
  // so `z.uuid()` would reject it while the app is perfectly healthy.
  const SEEDED_ID = '33333333-0000-0000-0000-000000000003';

  it('accepts a seeded, non v4 classroom id', () => {
    const parsed = ownedClassroomsSchema.parse([
      {
        id: SEEDED_ID,
        name: 'Seed Classroom (Dev)',
        joinCode: 'SEED23',
        studentCount: 1,
        quizCount: 2,
        createdAt: '2026-07-19T21:42:08.481847Z',
        archivedAt: null,
      },
    ]);

    expect(parsed[0]!.id).toBe(SEEDED_ID);
    expect(parsed[0]!.archivedAt).toBeNull();
  });

  it('accepts an archived class, which carries a timestamp instead of null', () => {
    const parsed = ownedClassroomsSchema.parse([
      {
        id: SEEDED_ID,
        name: 'Retired class',
        joinCode: 'ABC234',
        studentCount: 0,
        quizCount: 0,
        createdAt: '2026-07-19T21:42:08.481847Z',
        archivedAt: '2026-07-19T22:00:00Z',
      },
    ]);

    expect(parsed[0]!.archivedAt).toBe('2026-07-19T22:00:00Z');
  });

  it('accepts the enrolled list, which deliberately carries only id and name', () => {
    const parsed = enrolledClassroomsSchema.parse([{ id: SEEDED_ID, name: 'Biology 101' }]);
    expect(parsed).toHaveLength(1);
    expect(parsed[0]!.name).toBe('Biology 101');
  });

  it('parses a join preview, including the two flags the screen branches on', () => {
    const parsed = joinPreviewSchema.parse({
      classroomId: SEEDED_ID,
      name: 'Biology 101',
      alreadyEnrolled: true,
      isOwner: false,
    });

    expect(parsed.alreadyEnrolled).toBe(true);
    expect(parsed.isOwner).toBe(false);
  });

  it('rejects a body that is missing a field the screen relies on', () => {
    // A backend that stopped sending studentCount would otherwise render "undefined students".
    expect(() =>
      ownedClassroomsSchema.parse([
        { id: SEEDED_ID, name: 'X', joinCode: 'ABC234', quizCount: 0, createdAt: '2026-07-19T00:00:00Z' },
      ]),
    ).toThrow();
  });
});
