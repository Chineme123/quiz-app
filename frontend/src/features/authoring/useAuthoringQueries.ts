import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { qk } from '@/lib/api/queryKeys';
import {
  acceptDrafts,
  addQuestion,
  createQuiz,
  deleteQuestion,
  discardDrafts,
  editQuestion,
  generateQuestions,
  getClassroomQuizzes,
  getDrafts,
  getQuiz,
  publishQuiz,
  unpublishQuiz,
  type GenerateInput,
  type PublishInput,
  type QuestionInput,
} from './authoring.api';
import type { AuthoredQuiz, GeneratedDraft } from './authoring.schemas';

export function useClassroomQuizzes(classroomId: string) {
  return useQuery({
    queryKey: qk.classroomQuizzes(classroomId),
    queryFn: () => getClassroomQuizzes(classroomId),
    enabled: classroomId !== '',
  });
}

export function useQuiz(quizId: string) {
  return useQuery({
    queryKey: qk.quiz(quizId),
    queryFn: () => getQuiz(quizId),
    enabled: quizId !== '',
  });
}

export function useCreateQuiz(classroomId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ title, durationMinutes }: { title: string; durationMinutes: number }) =>
      createQuiz(classroomId, title, durationMinutes),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: qk.classroomQuizzes(classroomId) });
      // The dashboard shows a quiz count per class.
      void queryClient.invalidateQueries({ queryKey: qk.ownedClassrooms });
    },
  });
}

/**
 * Every write that answers with the whole quiz primes the cache with it rather than refetching,
 * and refreshes the class list whose counts it changed.
 */
function useQuizWrite<TArgs>(
  quizId: string,
  classroomId: string | undefined,
  mutationFn: (args: TArgs) => Promise<AuthoredQuiz>,
) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn,
    onSuccess: (quiz) => {
      queryClient.setQueryData(qk.quiz(quizId), quiz);
      if (classroomId !== undefined) {
        void queryClient.invalidateQueries({ queryKey: qk.classroomQuizzes(classroomId) });
      }
    },
  });
}

export function useAddQuestion(quizId: string, classroomId?: string) {
  return useQuizWrite(quizId, classroomId, (question: QuestionInput) => addQuestion(quizId, question));
}

export function useEditQuestion(quizId: string, classroomId?: string) {
  return useQuizWrite(
    quizId,
    classroomId,
    ({ questionId, question }: { questionId: string; question: QuestionInput }) =>
      editQuestion(quizId, questionId, question),
  );
}

export function usePublishQuiz(quizId: string, classroomId?: string) {
  return useQuizWrite(quizId, classroomId, (input: PublishInput) => publishQuiz(quizId, input));
}

export function useUnpublishQuiz(quizId: string, classroomId?: string) {
  return useQuizWrite(quizId, classroomId, () => unpublishQuiz(quizId));
}

/** The batch waiting for review, if there is one. */
export function useDrafts(quizId: string) {
  return useQuery({
    queryKey: qk.quizDrafts(quizId),
    queryFn: () => getDrafts(quizId),
    enabled: quizId !== '',
  });
}

/** Generating replaces whatever batch was pending, so the answer is the whole new batch. */
export function useGenerateQuestions(quizId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: GenerateInput) => generateQuestions(quizId, input),
    onSuccess: (draft: GeneratedDraft) => {
      queryClient.setQueryData(qk.quizDrafts(quizId), draft);
    },
  });
}

/**
 * Accepting promotes the chosen candidates and clears the whole batch, so the pending batch is
 * emptied here too rather than left showing candidates the server has already thrown away.
 */
export function useAcceptDrafts(quizId: string, classroomId?: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (draftIds: string[]) => acceptDrafts(quizId, draftIds),
    onSuccess: (quiz) => {
      queryClient.setQueryData(qk.quiz(quizId), quiz);
      queryClient.setQueryData(qk.quizDrafts(quizId), null);
      if (classroomId !== undefined) {
        void queryClient.invalidateQueries({ queryKey: qk.classroomQuizzes(classroomId) });
      }
    },
  });
}

export function useDiscardDrafts(quizId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => discardDrafts(quizId),
    onSuccess: () => {
      queryClient.setQueryData(qk.quizDrafts(quizId), null);
    },
  });
}

/** Delete answers with 204, so there is nothing to prime; refetch instead. */
export function useDeleteQuestion(quizId: string, classroomId?: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (questionId: string) => deleteQuestion(quizId, questionId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: qk.quiz(quizId) });
      if (classroomId !== undefined) {
        void queryClient.invalidateQueries({ queryKey: qk.classroomQuizzes(classroomId) });
      }
    },
  });
}
