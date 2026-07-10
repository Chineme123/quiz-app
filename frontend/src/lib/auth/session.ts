import { z } from 'zod';

export const roleSchema = z.enum(['Student', 'Teacher']);
export type Role = z.infer<typeof roleSchema>;

/**
 * The shape every auth endpoint returns (register, login, refresh). Responses
 * are camelCase (verified against the live backend). Validating here means a
 * drifting backend fails loud at the boundary instead of somewhere downstream.
 */
export const authSessionSchema = z.object({
  token: z.string().min(1),
  userId: z.uuid(),
  role: roleSchema,
});

export type AuthSession = z.infer<typeof authSessionSchema>;
