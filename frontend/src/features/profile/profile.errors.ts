// PUT /api/profile answers a validation failure with a bare string[] (UserService's
// ValidationResult.Errors). Each message is prefixed with the offending field name,
// so we route it back to that field and show it inline (AC-12). Anything unrecognised
// falls through to a form-level banner rather than being swallowed.
//
// This is deliberately the brittle seam the spec's follow-up calls out: when the
// backend moves to structured {field, message} errors, this prefix matching goes away.

export type ProfileFieldKey = 'displayName' | 'academicLevel' | 'instructorType';

export interface MappedProfileErrors {
  /** Field-scoped messages, keyed by form field. */
  fields: Partial<Record<ProfileFieldKey, string>>;
  /** Messages that matched no known field. */
  form: string[];
}

const FIELD_PREFIXES: ReadonlyArray<readonly [RegExp, ProfileFieldKey]> = [
  [/^displayname/i, 'displayName'],
  [/^academiclevel/i, 'academicLevel'],
  [/^instructortype/i, 'instructorType'],
];

export function mapProfileErrors(errors: readonly string[]): MappedProfileErrors {
  const fields: MappedProfileErrors['fields'] = {};
  const form: string[] = [];

  for (const message of errors) {
    const match = FIELD_PREFIXES.find(([prefix]) => prefix.test(message.trimStart()));
    if (match) {
      const key = match[1];
      // Keep the first message per field; later duplicates don't overwrite it.
      fields[key] ??= message;
    } else {
      form.push(message);
    }
  }

  return { fields, form };
}
