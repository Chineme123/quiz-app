import { ApiError, AuthError, ContractError, NetworkError, ValidationError } from './errors';

/** The server's error envelope for auth endpoints is `{ error: "..." }`. */
function serverError(body: unknown): string | null {
  if (body !== null && typeof body === 'object' && 'error' in body) {
    const value: unknown = body.error;
    if (typeof value === 'string' && value.trim() !== '') return value;
  }
  return null;
}

/**
 * Turn any thrown error into one safe, human line for a form banner. Sentence
 * case, no internal detail (security.md §6). Server-supplied messages are
 * already user-facing (AuthController writes them); everything else falls back.
 */
export function toUserMessage(
  error: unknown,
  fallback = 'Something went wrong. Please try again.',
): string {
  // NetworkError and AuthError already carry a friendly, safe message.
  if (error instanceof NetworkError || error instanceof AuthError) return error.message;
  // A bare string[] body (profile validation) — show the first, callers usually map the rest to fields.
  if (error instanceof ValidationError) return error.errors[0] ?? fallback;
  if (error instanceof ApiError) return serverError(error.body) ?? fallback;
  // A drifting 2xx contract is our bug, not the user's — keep it generic.
  if (error instanceof ContractError) return fallback;
  return fallback;
}
