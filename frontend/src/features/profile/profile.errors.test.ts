import { describe, it, expect } from 'vitest';
import { mapProfileErrors } from './profile.errors';

// AC-12: the server answers a bad PUT /api/profile with a bare string[]. Each message
// must land on the field it names; anything unrecognised falls through to the form.
describe('mapProfileErrors', () => {
  it('routes the display-name message to the displayName field', () => {
    const { fields, form } = mapProfileErrors(['DisplayName cannot be empty.']);
    expect(fields.displayName).toBe('DisplayName cannot be empty.');
    expect(form).toEqual([]);
  });

  it('routes the student role message to academicLevel', () => {
    const { fields } = mapProfileErrors(['AcademicLevel is required for students.']);
    expect(fields.academicLevel).toBe('AcademicLevel is required for students.');
    expect(fields.instructorType).toBeUndefined();
  });

  it('routes the teacher role message to instructorType', () => {
    const { fields } = mapProfileErrors(['InstructorType is required for teachers.']);
    expect(fields.instructorType).toBe('InstructorType is required for teachers.');
    expect(fields.academicLevel).toBeUndefined();
  });

  it('maps several messages at once, each to its own field', () => {
    const { fields, form } = mapProfileErrors([
      'DisplayName cannot be empty.',
      'AcademicLevel is required for students.',
    ]);
    expect(fields.displayName).toBe('DisplayName cannot be empty.');
    expect(fields.academicLevel).toBe('AcademicLevel is required for students.');
    expect(form).toEqual([]);
  });

  it('sends an unrecognised message to the form-level bucket, not a field', () => {
    const { fields, form } = mapProfileErrors(['Something unexpected went wrong.']);
    expect(fields).toEqual({});
    expect(form).toEqual(['Something unexpected went wrong.']);
  });

  it('keeps the first message when a field appears twice', () => {
    const { fields } = mapProfileErrors(['DisplayName cannot be empty.', 'DisplayName is too short.']);
    expect(fields.displayName).toBe('DisplayName cannot be empty.');
  });

  it('is case-insensitive and tolerates leading whitespace', () => {
    const { fields } = mapProfileErrors(['  displayname must be provided.']);
    expect(fields.displayName).toBe('  displayname must be provided.');
  });
});
