import { z } from 'zod';

/**
 * A .NET `Guid` as it arrives over the wire.
 *
 * Deliberately NOT `z.uuid()`. That enforces the RFC 4122 version and variant nibbles, but a
 * .NET Guid is any 128 bit value and the API never promised otherwise: the seeded ids look like
 * `44444444-0000-0000-0000-000000000004`, which is a perfectly valid Guid the server really
 * sends, and `z.uuid()` rejects it. Using it took the whole quiz list down (spec 0006) while
 * every test stayed green, because the tests mock the api module, so the schema never ran.
 *
 * Validate the shape the server actually sends, not a stricter one we assumed.
 */
export const guid = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/, 'Expected a GUID');
