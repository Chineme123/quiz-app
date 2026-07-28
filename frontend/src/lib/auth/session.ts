import { z } from 'zod';
import { guid } from '@/lib/api/schemas';

export const roleSchema = z.enum(['Student', 'Teacher']);
export type Role = z.infer<typeof roleSchema>;

/**
 * The shape every auth endpoint returns (register, login, refresh). Responses
 * are camelCase (verified against the live backend). Validating here means a
 * drifting backend fails loud at the boundary instead of somewhere downstream.
 *
 * `userId` is the local `guid`, never `z.uuid()`: the server sends a plain .NET Guid,
 * and the seeded ids (e.g. `22222222-0000-0000-0000-000000000002`) have a zero version
 * nibble, so `z.uuid()` rejects them and every seed account fails to sign in on a valid
 * 200 — the same spec 0006 trap the api schemas already switched away from.
 */
export const authSessionSchema = z.object({
  token: z.string().min(1),
  userId: guid,
  role: roleSchema,
});

export type AuthSession = z.infer<typeof authSessionSchema>;
