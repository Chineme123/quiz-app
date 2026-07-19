import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { qk } from '@/lib/api/queryKeys';
import {
  archiveClassroom,
  createClassroom,
  getClassroom,
  getClassroomRoster,
  getEnrolledClassrooms,
  getOwnedClassrooms,
  joinClassroom,
  leaveClassroom,
  previewClassroomByCode,
  regenerateJoinCode,
  removeStudent,
  renameClassroom,
  unarchiveClassroom,
} from './classrooms.api';

/** The teacher dashboard's list (AC-2). */
export function useOwnedClassrooms() {
  return useQuery({ queryKey: qk.ownedClassrooms, queryFn: getOwnedClassrooms });
}

/** The student dashboard's list (AC-5). */
export function useEnrolledClassrooms() {
  return useQuery({ queryKey: qk.enrolledClassrooms, queryFn: getEnrolledClassrooms });
}

/**
 * Resolves a join link's code to the class it opens. Disabled until there is a code, and not
 * refetched on focus: the preview is a one moment answer the student is about to act on.
 */
export function useClassroomByCode(code: string) {
  return useQuery({
    queryKey: qk.classroomByCode(code),
    queryFn: () => previewClassroomByCode(code),
    enabled: code !== '',
    refetchOnWindowFocus: false,
  });
}

export function useCreateClassroom() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (name: string) => createClassroom(name),
    // The new class carries counts the create response does not, so refetch the list rather
    // than priming it with a half filled row.
    onSuccess: () => queryClient.invalidateQueries({ queryKey: qk.ownedClassrooms }),
  });
}

export function useJoinClassroom() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (code: string) => joinClassroom(code),
    onSuccess: () => {
      // Joining changes what this student can see and take, so both lists are now stale.
      void queryClient.invalidateQueries({ queryKey: qk.enrolledClassrooms });
      void queryClient.invalidateQueries({ queryKey: qk.availableQuizzes });
    },
  });
}

export function useLeaveClassroom() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (classroomId: string) => leaveClassroom(classroomId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: qk.enrolledClassrooms });
      void queryClient.invalidateQueries({ queryKey: qk.availableQuizzes });
    },
  });
}

/** One class in detail. Null means it is not yours and you are not in it (AC-11). */
export function useClassroom(classroomId: string) {
  return useQuery({
    queryKey: qk.classroom(classroomId),
    queryFn: () => getClassroom(classroomId),
    enabled: classroomId !== '',
  });
}

/** The owner's roster, one page at a time (AC-10). */
export function useClassroomRoster(classroomId: string, page: number) {
  return useQuery({
    queryKey: qk.classroomRoster(classroomId, page),
    queryFn: () => getClassroomRoster(classroomId, page),
    enabled: classroomId !== '',
  });
}

/**
 * The owner's management actions. Each refetches the class and the owned list, because a rename,
 * an archive, or a new code changes what both the detail screen and the dashboard show.
 */
function useClassroomMutation<TArgs>(mutationFn: (args: TArgs) => Promise<unknown>, classroomId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: qk.classroom(classroomId) });
      void queryClient.invalidateQueries({ queryKey: qk.ownedClassrooms });
    },
  });
}

export function useRenameClassroom(classroomId: string) {
  return useClassroomMutation((name: string) => renameClassroom(classroomId, name), classroomId);
}

export function useArchiveClassroom(classroomId: string) {
  return useClassroomMutation(() => archiveClassroom(classroomId), classroomId);
}

export function useUnarchiveClassroom(classroomId: string) {
  return useClassroomMutation(() => unarchiveClassroom(classroomId), classroomId);
}

export function useRegenerateJoinCode(classroomId: string) {
  return useClassroomMutation(() => regenerateJoinCode(classroomId), classroomId);
}

export function useRemoveStudent(classroomId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (studentId: string) => removeStudent(classroomId, studentId),
    onSuccess: () => {
      // The roster is paged, so drop every page rather than guess which one changed.
      void queryClient.invalidateQueries({ queryKey: ['classroomRoster', classroomId] });
      void queryClient.invalidateQueries({ queryKey: qk.classroom(classroomId) });
      void queryClient.invalidateQueries({ queryKey: qk.ownedClassrooms });
    },
  });
}
