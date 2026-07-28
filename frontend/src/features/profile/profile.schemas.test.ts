import { describe, it, expect } from 'vitest';
import { profileSchema } from './profile.schemas';

/**
 * The profile wire contract, parsed against the ids the server really sends. The ManageProfile page
 * test mocks the api, so this schema never runs there; a `userId` shape it is too strict for would
 * only fail in the browser on a real 200 — the same spec 0006 trap that broke sign in, since the
 * profile userId is the same plain .NET Guid.
 */
describe('profileSchema accepts the ids the server actually sends', () => {
  // The seed student id: a valid .NET Guid, non v4 (zero version nibble), the case `z.uuid()` rejected.
  const SEEDED_ID = '22222222-0000-0000-0000-000000000002';

  const base = {
    userId: SEEDED_ID,
    displayName: 'Sam Carter',
    createdAt: '2026-07-28T18:46:30.008574Z',
    updatedAt: '2026-07-28T18:46:30.008574Z',
  };

  it('parses a profile for a seeded, non v4 user id', () => {
    const parsed = profileSchema.parse(base);
    expect(parsed.userId).toBe(SEEDED_ID);
    expect(parsed.displayName).toBe('Sam Carter');
  });

  it('rejects a userId that is not a GUID at all', () => {
    expect(() => profileSchema.parse({ ...base, userId: 'nope' })).toThrow();
  });
});
