import { useQuery } from '@tanstack/react-query';
import { qk } from '@/lib/api/queryKeys';
import { getAttemptResult } from './results.api';

/**
 * The student's attempt result. The score and per question breakdown are available as
 * soon as the attempt is graded; feedback arrives shortly after, generated in the
 * background. While feedback is still Pending this polls the result endpoint every two
 * seconds and stops the moment it is Ready (spec 0005, AC-12).
 */
export function useAttemptResult(attemptId: string) {
  return useQuery({
    queryKey: qk.attemptResult(attemptId),
    queryFn: () => getAttemptResult(attemptId),
    enabled: attemptId !== '',
    // Poll ONLY while feedback is still generating. Any other state stops the timer:
    // Ready (done), null (a 404 not-found), or an error, so a bad result never polls
    // forever. The first load happens regardless; refetchInterval governs refetches.
    refetchInterval: (query) => (query.state.data?.feedbackStatus === 'Pending' ? 2000 : false),
  });
}
