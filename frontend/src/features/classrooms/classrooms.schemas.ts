import { z } from 'zod';
import { guid } from '@/lib/api/schemas';

/**
 * Wire shapes for the classroom endpoints (spec 0008).
 *
 * Ids use the local `guid` primitive, never `z.uuid()`: the Assessment module emits plain .NET
 * Guids (the seeded ones are not v4), and the stricter check took the quiz list down once while
 * every mocked test stayed green. See `lib/api/schemas.ts`.
 */

/** A class as its owning teacher sees it, with the counts the dashboard shows. */
export const ownedClassroomSchema = z.object({
  id: guid,
  name: z.string(),
  joinCode: z.string(),
  studentCount: z.number(),
  quizCount: z.number(),
  createdAt: z.string(),
  archivedAt: z.string().nullish(),
});
export const ownedClassroomsSchema = z.array(ownedClassroomSchema);
export type OwnedClassroom = z.infer<typeof ownedClassroomSchema>;

/**
 * A class a student has joined. Just the name for now: the teacher's display name lives in the
 * Identity module behind a plain Guid, so showing it would need a cross module read (spec 0007).
 */
export const enrolledClassroomSchema = z.object({
  id: guid,
  name: z.string(),
});
export const enrolledClassroomsSchema = z.array(enrolledClassroomSchema);
export type EnrolledClassroom = z.infer<typeof enrolledClassroomSchema>;

/** What creating a class returns. */
export const classroomSchema = z.object({
  id: guid,
  name: z.string(),
  joinCode: z.string(),
  createdAt: z.string(),
  archivedAt: z.string().nullish(),
});
export type Classroom = z.infer<typeof classroomSchema>;

/** What the join screen shows before the student commits, so a link never enrols silently. */
export const joinPreviewSchema = z.object({
  classroomId: guid,
  name: z.string(),
  alreadyEnrolled: z.boolean(),
  isOwner: z.boolean(),
});
export type JoinPreview = z.infer<typeof joinPreviewSchema>;

/**
 * One class in detail. The owner gets the join code and counts; a participant gets the name
 * only, so holding a place in a class is never permission to invite others into it.
 */
export const classroomDetailSchema = z.object({
  id: guid,
  name: z.string(),
  isOwner: z.boolean(),
  joinCode: z.string().nullish(),
  archivedAt: z.string().nullish(),
  studentCount: z.number().nullish(),
  quizCount: z.number().nullish(),
});
export type ClassroomDetail = z.infer<typeof classroomDetailSchema>;

/** The owner's roster, paged like every other list in the module. */
export const rosterEntrySchema = z.object({
  studentId: guid,
  enrolledAt: z.string(),
});

export const classroomRosterSchema = z.object({
  items: z.array(rosterEntrySchema),
  total: z.number(),
  page: z.number(),
  pageSize: z.number(),
});
export type ClassroomRoster = z.infer<typeof classroomRosterSchema>;
export type RosterEntry = z.infer<typeof rosterEntrySchema>;

/** What regenerating a join code returns. */
export const regeneratedCodeSchema = z.object({
  id: guid,
  joinCode: z.string(),
});

/** What a successful join returns. Already being in the class is also a success. */
export const joinResultSchema = z.object({
  classroomId: guid,
  name: z.string(),
});
export type JoinResult = z.infer<typeof joinResultSchema>;
