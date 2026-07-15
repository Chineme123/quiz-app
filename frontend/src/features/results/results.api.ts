import { apiFetch } from '@/lib/api/client';
import { ApiError } from '@/lib/api/errors';
import { attemptResultSchema } from './results.schemas';
import type { AttemptResult } from './results.schemas';

/**
 * Load a student's own attempt result. A 404 means the attempt does not exist or is
 * not the caller's (the backend returns 404 either way so it never reveals another
 * student's work, AC-9); the screen shows a "not found" state. Every other failure
 * propagates to the error state.
 */
export async function getAttemptResult(attemptId: string): Promise<AttemptResult | null> {
  try {
    return await apiFetch(`/api/attempts/${attemptId}/result`, { schema: attemptResultSchema });
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) return null;
    throw error;
  }
}
