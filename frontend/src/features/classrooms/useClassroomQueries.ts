import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { qk } from '@/lib/api/queryKeys';
import {
  createClassroom,
  getEnrolledClassrooms,
  getOwnedClassrooms,
  joinClassroom,
  leaveClassroom,
  previewClassroomByCode,
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
