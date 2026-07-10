import { apiFetch } from '@/lib/api/client';
import { ApiError } from '@/lib/api/errors';
import { profileSchema } from './profile.schemas';
import type { Profile, ProfileUpdateRequest } from './profile.schemas';

/**
 * Load the current user's profile. A 404 is not an error here: it means the row
 * does not exist yet (the first time a user opens this screen), which the UI shows
 * as an empty form (AC-10). Every other failure propagates.
 */
export async function getProfile(): Promise<Profile | null> {
  try {
    return await apiFetch('/api/profile', { schema: profileSchema });
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) return null;
    throw error;
  }
}

/** Create-or-update the profile. On validation failure the client throws a
 *  ValidationError carrying the server's string[], which the screen maps to fields. */
export function updateProfile(request: ProfileUpdateRequest): Promise<Profile> {
  return apiFetch('/api/profile', { method: 'PUT', json: request, schema: profileSchema });
}
