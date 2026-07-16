import { useEffect, useRef, useState } from 'react';

/**
 * Seconds left until the attempt's deadline (spec 0006, AC-10).
 *
 * It counts down on the gap the SERVER reported, not on the device clock: we take the offset
 * between the server's now and ours once, at load, and apply it from then on. A student whose
 * laptop clock is days out still sees the true time remaining. The server is the judge either
 * way, so this only keeps the display honest, but a timer that visibly lies during a graded
 * quiz is its own kind of failure.
 *
 * Returns null until the clock is known, so callers can render nothing rather than a wrong zero.
 */
export function useCountdown(expiresAt: string | undefined, serverNow: string | undefined): number | null {
  const [secondsLeft, setSecondsLeft] = useState<number | null>(null);

  // The gap between the server's clock and this device's, measured once. A ref, not state:
  // re-measuring on every render would let a drifting device clock creep into the countdown.
  const offsetMs = useRef<number | null>(null);

  useEffect(() => {
    if (!expiresAt || !serverNow) return;

    if (offsetMs.current === null) {
      offsetMs.current = new Date(serverNow).getTime() - Date.now();
    }

    const deadline = new Date(expiresAt).getTime();
    const tick = () => {
      const serverTime = Date.now() + (offsetMs.current ?? 0);
      setSecondsLeft(Math.max(0, Math.round((deadline - serverTime) / 1000)));
    };

    tick();
    const id = window.setInterval(tick, 1000);
    return () => window.clearInterval(id);
  }, [expiresAt, serverNow]);

  return secondsLeft;
}

/** Formats seconds as m:ss, for reading at a glance under time pressure. */
export function formatCountdown(totalSeconds: number): string {
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return `${minutes}:${String(seconds).padStart(2, '0')}`;
}
