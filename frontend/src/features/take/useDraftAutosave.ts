import { useCallback, useEffect, useRef, useState } from 'react';
import { ApiError } from '@/lib/api/errors';
import { saveDraftAnswers } from './take.api';

/** What the student is told about their work being safe (spec 0006, AC-8). */
export type SaveState = 'idle' | 'saving' | 'saved' | 'retrying' | 'failed' | 'closed';

const DEBOUNCE_MS = 500;
const RETRY_MS = 3000;

/**
 * Saves the answer set to the server as the student works (spec 0006, AC-6, AC-8).
 *
 * Debounced: picking an option saves about half a second later, so a burst of typing in a short
 * answer is one write, not one per keystroke. It always sends the WHOLE set, never a delta, so
 * two saves in flight cannot interleave and drop an answer; the last write simply wins.
 *
 * A failed save keeps retrying quietly in the background and the answer stays on screen. The
 * state it reports is the honest one: we never say "saved" until the server said so, because the
 * whole point of this feature is that a student can trust their work is safe.
 *
 * A 409 means the attempt takes no more writes (time is up, or it is already submitted). That is
 * not a retryable failure, so it stops and reports `closed`.
 */
export function useDraftAutosave(attemptId: string, enabled: boolean) {
  const [saveState, setSaveState] = useState<SaveState>('idle');

  // The latest answers, and whether they still need writing. Refs, because the debounce timer
  // and the retry both need to read the CURRENT set at the moment they fire, not the set that
  // existed when they were scheduled.
  const pending = useRef<Record<string, string> | null>(null);
  const timer = useRef<number | null>(null);
  const inFlight = useRef(false);
  // The retry needs to call the CURRENT flush, but flush cannot reference itself while it is
  // still being declared. A ref, kept pointing at the latest one, breaks the cycle.
  const flushRef = useRef<() => Promise<void>>(async () => {});

  const flush = useCallback(async () => {
    if (!enabled || inFlight.current) return;
    const answers = pending.current;
    if (!answers) return;

    inFlight.current = true;
    pending.current = null;
    setSaveState((s) => (s === 'retrying' ? 'retrying' : 'saving'));

    try {
      await saveDraftAnswers(attemptId, answers);
      // Only report saved if nothing new arrived while this was in flight; otherwise the next
      // flush will report it, and claiming "saved" now would be a lie about the newer answer.
      setSaveState(pending.current ? 'saving' : 'saved');
    } catch (error) {
      if (error instanceof ApiError && error.status === 409) {
        // Not retryable: this attempt is finished or out of time.
        setSaveState('closed');
        pending.current = null;
        return;
      }
      // Put the work back and try again shortly. The answer stays on screen throughout.
      pending.current = answers;
      setSaveState('retrying');
      timer.current = window.setTimeout(() => void flushRef.current(), RETRY_MS);
    } finally {
      inFlight.current = false;
    }
  }, [attemptId, enabled]);

  useEffect(() => {
    flushRef.current = flush;
  }, [flush]);

  /** Queue the whole current answer set to be saved. */
  const save = useCallback(
    (answers: Record<string, string>) => {
      if (!enabled) return;
      pending.current = answers;
      if (timer.current) window.clearTimeout(timer.current);
      timer.current = window.setTimeout(() => void flushRef.current(), DEBOUNCE_MS);
    },
    [enabled],
  );

  /** Write immediately, skipping the debounce. Used before submitting. */
  const flushNow = useCallback(async () => {
    if (timer.current) window.clearTimeout(timer.current);
    await flush();
  }, [flush]);

  useEffect(() => () => {
    if (timer.current) window.clearTimeout(timer.current);
  }, []);

  return { saveState, save, flushNow };
}
