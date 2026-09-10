import { describe, it, expect } from 'vitest';
import { utcIsoToLocalInput, localInputToUtcIso } from './datetime';

/**
 * These guard the availability-window round-trip. The exact UTC offset depends on the machine's
 * zone (CI runs in UTC, a developer may not), so the assertions are written to hold in ANY zone:
 * the local -> UTC -> local round-trip is the identity, and a UTC instant always ends in `Z`.
 */
describe('availability-window datetime conversions', () => {
  it('round-trips a local datetime through UTC unchanged (any timezone)', () => {
    const local = '2026-09-10T10:20';
    expect(utcIsoToLocalInput(localInputToUtcIso(local))).toBe(local);
  });

  it('sends a zone-aware UTC instant, not the naive wall-clock', () => {
    const iso = localInputToUtcIso('2026-09-10T10:20');
    expect(iso).not.toBeNull();
    expect(iso!.endsWith('Z')).toBe(true); // a real instant, so the server compares it correctly
    // The same instant read back is the local time the teacher typed.
    expect(utcIsoToLocalInput(iso)).toBe('2026-09-10T10:20');
  });

  it('treats a blank input as no bound (null), both ways', () => {
    expect(localInputToUtcIso('')).toBeNull();
    expect(utcIsoToLocalInput(null)).toBe('');
    expect(utcIsoToLocalInput('')).toBe('');
  });

  it('never renders "Invalid Date" from a bad stored value', () => {
    expect(utcIsoToLocalInput('not-a-date')).toBe('');
    expect(localInputToUtcIso('not-a-date')).toBeNull();
  });
});
