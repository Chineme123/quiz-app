import { apiFetch } from '@/lib/api/client';
import { myResultsSchema } from './myResults.schemas';
import type { MyResults } from './myResults.schemas';

/**
 * The signed in student's own results (spec 0011). The endpoint takes no id — the caller comes from
 * the token — so it can only ever return your own results (AC-1). It always answers 200: an empty
 * result is a normal payload with no classrooms, not a 404, because a student always owns their own
 * results (AC-8). So there is no 404-to-null mapping here, unlike the owner scoped teacher reads.
 */
export async function getMyResults(): Promise<MyResults> {
  return apiFetch('/api/results/mine', { schema: myResultsSchema });
}
