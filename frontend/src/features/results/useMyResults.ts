import { useQuery } from '@tanstack/react-query';
import { qk } from '@/lib/api/queryKeys';
import { getMyResults } from './myResults.api';

/** The signed in student's own results, grouped by class (spec 0011, AC-1, AC-2). */
export function useMyResults() {
  return useQuery({
    queryKey: qk.myResults,
    queryFn: getMyResults,
  });
}
