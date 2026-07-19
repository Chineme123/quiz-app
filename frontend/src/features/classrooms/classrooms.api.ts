import { apiFetch } from '@/lib/api/client';
import { ApiError } from '@/lib/api/errors';
import {
  classroomSchema,
  enrolledClassroomsSchema,
  joinPreviewSchema,
  joinResultSchema,
  ownedClassroomsSchema,
} from './classrooms.schemas';
import type {
  Classroom,
  EnrolledClassroom,
  JoinPreview,
  JoinResult,
  OwnedClassroom,
} from './classrooms.schemas';

/** The classes this teacher owns. Scoped by the server, so there is nothing to pass. */
export async function getOwnedClassrooms(): Promise<OwnedClassroom[]> {
  return apiFetch('/api/classrooms/owned', { schema: ownedClassroomsSchema });
}

/** The classes this user has joined. Archived ones drop off the list (AC-8). */
export async function getEnrolledClassrooms(): Promise<EnrolledClassroom[]> {
  return apiFetch('/api/classrooms/enrolled', { schema: enrolledClassroomsSchema });
}

/** Creates a class. The server issues the join code; only a teacher may call this (AC-1). */
export async function createClassroom(name: string): Promise<Classroom> {
  return apiFetch('/api/classrooms', {
    method: 'POST',
    json: { name },
    schema: classroomSchema,
  });
}

/**
 * Resolves a join code to the class it opens, so the join screen can name it before the student
 * commits (AC-4). A 404 means no active class carries that code, which includes an archived one:
 * the server answers the same either way, so a code never reveals that a class exists.
 */
export async function previewClassroomByCode(code: string): Promise<JoinPreview | null> {
  try {
    return await apiFetch(`/api/classrooms/by-code/${encodeURIComponent(code)}`, {
      schema: joinPreviewSchema,
    });
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) return null;
    throw error;
  }
}

/**
 * Joins by code. Already being enrolled is a success, not an error, so a double submit or a
 * reopened link is harmless (AC-3). A 404 is an unknown or archived code; a 409 is your own class.
 */
export async function joinClassroom(code: string): Promise<JoinResult> {
  return apiFetch('/api/classrooms/join', {
    method: 'POST',
    json: { code },
    schema: joinResultSchema,
  });
}

/** Leaves a class. Idempotent, and it keeps every past attempt and result (AC-9). */
export async function leaveClassroom(classroomId: string): Promise<void> {
  await apiFetch(`/api/classrooms/${classroomId}/leave`, { method: 'POST' });
}
