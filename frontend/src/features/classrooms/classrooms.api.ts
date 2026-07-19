import { apiFetch } from '@/lib/api/client';
import { ApiError } from '@/lib/api/errors';
import {
  classroomDetailSchema,
  classroomRosterSchema,
  classroomSchema,
  enrolledClassroomsSchema,
  joinPreviewSchema,
  joinResultSchema,
  ownedClassroomsSchema,
  regeneratedCodeSchema,
} from './classrooms.schemas';
import type {
  Classroom,
  ClassroomDetail,
  ClassroomRoster,
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

/**
 * One class in detail. A 404 covers both "no such class" and "not yours and you are not in it",
 * because the server answers the same either way (AC-11).
 */
export async function getClassroom(classroomId: string): Promise<ClassroomDetail | null> {
  try {
    return await apiFetch(`/api/classrooms/${classroomId}`, { schema: classroomDetailSchema });
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) return null;
    throw error;
  }
}

/** The owner's roster, paged (AC-10). */
export async function getClassroomRoster(
  classroomId: string,
  page: number,
  pageSize = 20,
): Promise<ClassroomRoster> {
  return apiFetch(`/api/classrooms/${classroomId}/students?page=${page}&pageSize=${pageSize}`, {
    schema: classroomRosterSchema,
  });
}

/** Renames a class. Owner only; a non owner gets a 404 rather than a refusal (AC-7). */
export async function renameClassroom(classroomId: string, name: string): Promise<void> {
  await apiFetch(`/api/classrooms/${classroomId}`, { method: 'PATCH', json: { name } });
}

/** Archives a class. Reversible, and it destroys nothing (AC-8). */
export async function archiveClassroom(classroomId: string): Promise<void> {
  await apiFetch(`/api/classrooms/${classroomId}/archive`, { method: 'POST' });
}

export async function unarchiveClassroom(classroomId: string): Promise<void> {
  await apiFetch(`/api/classrooms/${classroomId}/unarchive`, { method: 'POST' });
}

/** Issues a new join code. The old code and its link stop working immediately (AC-7). */
export async function regenerateJoinCode(classroomId: string): Promise<string> {
  const { joinCode } = await apiFetch(`/api/classrooms/${classroomId}/regenerate-code`, {
    method: 'POST',
    schema: regeneratedCodeSchema,
  });
  return joinCode;
}

/** Removes a student's enrolment. Keeps their past attempts and results (AC-7). */
export async function removeStudent(classroomId: string, studentId: string): Promise<void> {
  await apiFetch(`/api/classrooms/${classroomId}/students/${studentId}`, { method: 'DELETE' });
}
