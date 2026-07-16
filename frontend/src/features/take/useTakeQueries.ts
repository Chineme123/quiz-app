import { useQuery } from '@tanstack/react-query';
import { qk } from '@/lib/api/queryKeys';
import { getAttemptQuestions, getAvailableQuizzes } from './take.api';

/** The quizzes this student may take (spec 0006, AC-1). Scoped server side by enrolment. */
export function useAvailableQuizzes() {
  return useQuery({
    queryKey: qk.availableQuizzes,
    queryFn: getAvailableQuizzes,
  });
}

/**
 * One attempt's questions, deadline, server clock, and saved answers (spec 0006, AC-4, AC-9).
 * Fetched once per attempt: the questions do not change under a running attempt, and refetching
 * would reset the server clock offset the countdown is built on.
 */
export function useAttemptQuestions(attemptId: string) {
  return useQuery({
    queryKey: qk.attemptQuestions(attemptId),
    queryFn: () => getAttemptQuestions(attemptId),
    enabled: attemptId !== '',
    staleTime: Infinity,
    refetchOnWindowFocus: false,
  });
}
