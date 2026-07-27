/**
 * Small display helpers for the results screens. There is no shared number formatter in the app,
 * and these are specific to how results read, so they live with the slice. Nulls (no data) are
 * handled at the call site with words, never a bare placeholder, so a cell always reads plainly.
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
