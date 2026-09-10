/**
 * Availability-window conversions for the quiz editor (spec 0009).
 *
 * A `datetime-local` input speaks the browser's LOCAL wall-clock and carries no zone; the server
 * stores and compares instants in UTC (the `timestamp with time zone` columns, `Quiz.CanStart` vs
 * `DateTime.UtcNow`). So the two must be bridged explicitly: send the true UTC instant for the time
 * the teacher picked, and show a stored UTC instant back in their local time. Treating the naive
 * value AS UTC (the first cut of the publish fix) shifted every window by the teacher's offset — a
 * 9am window became 9am UTC, i.e. 4am for a US-Central teacher, so their quiz was never visible.
 */

/** A stored UTC ISO instant shown as the browser's local `YYYY-MM-DDTHH:mm` for a datetime-local input. */
export function utcIsoToLocalInput(iso: string | null): string {
  if (iso === null || iso === '') return '';
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return '';
  const pad = (n: number) => String(n).padStart(2, '0');
  return (
    `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}` +
    `T${pad(date.getHours())}:${pad(date.getMinutes())}`
  );
}

/** A datetime-local value (the browser's local wall-clock) as a UTC ISO instant, or null when blank. */
export function localInputToUtcIso(local: string): string | null {
  if (local === '') return null;
  const date = new Date(local); // a zone-less date-time string parses as local time
  return Number.isNaN(date.getTime()) ? null : date.toISOString();
}
