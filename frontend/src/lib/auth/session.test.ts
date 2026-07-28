import { describe, it, expect } from 'vitest';
import { authSessionSchema } from './session';

/**
 * The auth wire contract, parsed against the ids the server really sends.
 *
 * Login, register, and refresh all validate through this schema at the boundary, but the page and
 * flow tests mock the api module, so the schema never runs in them. A `userId` shape the schema is
 * too strict for sails past every green test and only fails in the browser on a real 200 — which is
 * exactly how every seed account stopped signing in until this was fixed (the spec 0006 trap).
 */
describe('authSessionSchema accepts the ids the server actually sends', () => {
  // The seed student id from the dev database: a valid .NET Guid, but its zero version nibble makes
  // it a non v4 UUID, the case `z.uuid()` rejected while /api/auth/login returned a healthy 200.
  const SEEDED_ID = '22222222-0000-0000-0000-000000000002';
  // A Guid.NewGuid() style id (v4), as real registered accounts carry. Both must parse.
  const GENERATED_ID = 'b7f3c1a2-9d4e-4f6a-8b2c-1e5d7a9f0c3b';

  it('parses a session for a seeded, non v4 user id', () => {
    const parsed = authSessionSchema.parse({ token: 'a.b.c', userId: SEEDED_ID, role: 'Student' });
    expect(parsed.userId).toBe(SEEDED_ID);
    expect(parsed.role).toBe('Student');
  });

  it('parses a session for a generated v4 user id', () => {
    const parsed = authSessionSchema.parse({ token: 'a.b.c', userId: GENERATED_ID, role: 'Teacher' });
    expect(parsed.userId).toBe(GENERATED_ID);
  });

  it('rejects a userId that is not a GUID at all', () => {
    expect(() =>
      authSessionSchema.parse({ token: 'a.b.c', userId: 'not-a-guid', role: 'Student' }),
    ).toThrow();
  });

  it('rejects an empty token, so a blank session cannot pass', () => {
    expect(() => authSessionSchema.parse({ token: '', userId: SEEDED_ID, role: 'Student' })).toThrow();
  });

  it('rejects an unknown role', () => {
    expect(() =>
      authSessionSchema.parse({ token: 'a.b.c', userId: SEEDED_ID, role: 'Admin' }),
    ).toThrow();
  });
});
