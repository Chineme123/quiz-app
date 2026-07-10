import { z } from 'zod';
import type { Role } from '@/lib/auth/session';

// The dropdown vocabularies come straight from docs/uc14-ui-ux-brief.md. The server
// stores whatever string it is sent, so these values are the contract for this screen.
export const ACADEMIC_LEVELS = ['Freshman', 'Sophomore', 'Junior', 'Senior', 'Graduate'] as const;
export const INSTRUCTOR_TYPES = [
  'Professor',
  'Assistant Professor',
  'Teaching Assistant',
  'High School Teacher',
] as const;

export const ACADEMIC_LEVEL_OPTIONS = ACADEMIC_LEVELS.map((v) => ({ value: v, label: v }));
export const INSTRUCTOR_TYPE_OPTIONS = INSTRUCTOR_TYPES.map((v) => ({ value: v, label: v }));

/**
 * The wire shape of GET/PUT /api/profile (ProfileDTO, camelCase). Validated at the
 * boundary so a drifting backend fails loud here instead of somewhere downstream.
 */
export const profileSchema = z.object({
  userId: z.uuid(),
  displayName: z.string(),
  bio: z.string().nullish(),
  avatarUrl: z.string().nullish(),
  school: z.string().nullish(),
  department: z.string().nullish(),
  academicLevel: z.string().nullish(),
  instructorType: z.string().nullish(),
  createdAt: z.string(),
  updatedAt: z.string(),
});

export type Profile = z.infer<typeof profileSchema>;

/**
 * Client-side form rules. Only Display Name is enforced here (AC-11 instant
 * feedback); the conditional role field is validated server-side by the profile
 * update strategy, and its error is mapped back to the field (AC-12) rather than
 * duplicating that business rule in the browser.
 */
export const profileFormSchema = z.object({
  displayName: z.string().trim().min(1, 'Display name is required.'),
  bio: z.string(),
  avatarUrl: z.union([z.literal(''), z.url('Enter a valid image address, or leave it blank.')]),
  school: z.string(),
  department: z.string(),
  academicLevel: z.string(),
  instructorType: z.string(),
});

export type ProfileFormValues = z.infer<typeof profileFormSchema>;

/** The body of PUT /api/profile (ProfileUpdateRequest). Blank optionals become null. */
export interface ProfileUpdateRequest {
  displayName: string;
  bio: string | null;
  avatarUrl: string | null;
  school: string | null;
  department: string | null;
  academicLevel: string | null;
  instructorType: string | null;
}

/** Seed the form from a loaded profile, or all-blank for a first-time user (404 → null). */
export function toFormValues(profile: Profile | null): ProfileFormValues {
  return {
    displayName: profile?.displayName ?? '',
    bio: profile?.bio ?? '',
    avatarUrl: profile?.avatarUrl ?? '',
    school: profile?.school ?? '',
    department: profile?.department ?? '',
    academicLevel: profile?.academicLevel ?? '',
    instructorType: profile?.instructorType ?? '',
  };
}

const blankToNull = (value: string): string | null => {
  const trimmed = value.trim();
  return trimmed === '' ? null : trimmed;
};

/**
 * Build the update body for the caller's role: a student sends academicLevel and
 * never instructorType, a teacher the reverse (AC-10). The other field is nulled
 * so switching roles cannot leave a stale value behind.
 */
export function toUpdateRequest(values: ProfileFormValues, role: Role): ProfileUpdateRequest {
  const base = {
    displayName: values.displayName.trim(),
    bio: blankToNull(values.bio),
    avatarUrl: blankToNull(values.avatarUrl),
    school: blankToNull(values.school),
    department: blankToNull(values.department),
  };
  return role === 'Student'
    ? { ...base, academicLevel: blankToNull(values.academicLevel), instructorType: null }
    : { ...base, academicLevel: null, instructorType: blankToNull(values.instructorType) };
}
