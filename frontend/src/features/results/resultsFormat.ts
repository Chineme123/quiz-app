/**
 * Small display helpers for the student's own results screen (spec 0011). The classroom-results
 * slice keeps its own copy of the same idea by the same convention (these helpers live with the
 * slice that reads them, not in a shared module), so a student result reads plainly without the
 * results slice reaching across into another feature.
 */

/** A percentage rounded to a whole number, e.g. 66.7 -> "67%". */
export function percentLabel(value: number): string {
  return `${Math.round(value)}%`;
}

/** A points score out of the quiz total, e.g. "8 / 10". Whole-valued scores drop the decimal. */
export function scoreLabel(score: number, totalPoints: number): string {
  return `${trimDecimal(score)} / ${totalPoints}`;
}

/** Shows a whole number plainly and keeps at most one decimal for a real fraction (8, or 7.5). */
export function trimDecimal(value: number): string {
  return Number.isInteger(value) ? String(value) : String(Math.round(value * 10) / 10);
}

/** The submission date in a plain, friendly form, e.g. "27 Jul 2026". Unparseable input reads blank
 *  so a bad value never shows the word "Invalid Date". */
export function dateLabel(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return '';
  return date.toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' });
}
