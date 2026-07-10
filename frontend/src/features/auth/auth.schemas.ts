import { z } from 'zod';
import { roleSchema } from '@/lib/auth/session';

// Client-side form rules. The server is the final authority (it re-checks and owns
// uniqueness), but validating here gives instant, field-level feedback.

/** Sign in only needs a well-formed email and a non-empty password — never gate
 *  an existing account behind a rule its password may predate. */
export const signInSchema = z.object({
  email: z.email('Enter a valid email address.'),
  password: z.string().min(1, 'Enter your password.'),
});

/** Register adds a light password floor and the role the account is created with.
 *  The role values come from `roleSchema` so the two never drift; only the empty
 *  "not chosen yet" message is form-specific. */
export const registerSchema = z.object({
  email: z.email('Enter a valid email address.'),
  password: z.string().min(8, 'Use at least 8 characters.'),
  role: z.enum(roleSchema.options, { error: 'Select your role.' }),
});

export type SignInValues = z.infer<typeof signInSchema>;
export type RegisterValues = z.infer<typeof registerSchema>;
