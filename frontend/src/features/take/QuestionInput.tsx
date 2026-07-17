import type { AttemptQuestion } from './take.schemas';

interface QuestionInputProps {
  question: AttemptQuestion;
  value: string;
  onChange: (value: string) => void;
  disabled: boolean;
  /**
   * Id of the visible heading that shows the prompt. The radio group is labelled BY that
   * heading rather than repeating the prompt in a legend, so a screen reader reads the
   * question once, not twice.
   */
  labelledBy: string;
}

/**
 * The answer control for one question (spec 0006, AC-17). All three types the quiz supports:
 * multiple choice and true or false as radio groups, short answer as a text box.
 *
 * The stored answer is a string for every type, matching what the server grades: the option's
 * index for multiple choice, "true"/"false" for true or false, and the raw text for short answer.
 */
export function QuestionInput({ question, value, onChange, disabled, labelledBy }: QuestionInputProps) {
  if (question.questionType === 'ShortAnswerQuestion') {
    return (
      <div>
        <input
          id={`q-${question.id}`}
          aria-labelledby={labelledBy}
          type="text"
          value={value}
          disabled={disabled}
          onChange={(e) => onChange(e.target.value)}
          autoComplete="off"
          placeholder="Type your answer"
          className="w-full rounded-field border border-border bg-surface-card px-4 py-3 font-body text-text-strong
                     placeholder:text-text-muted focus:border-primary focus:outline-none focus:ring-2
                     focus:ring-primary/40 disabled:opacity-60"
        />
      </div>
    );
  }

  const choices =
    question.questionType === 'TrueFalseQuestion'
      ? [
          { value: 'true', label: 'True' },
          { value: 'false', label: 'False' },
        ]
      : (question.options ?? []).map((option, index) => ({ value: String(index), label: option }));

  return (
    <fieldset disabled={disabled} className="min-w-0" aria-labelledby={labelledBy}>
      <div className="flex flex-col gap-3">
        {choices.map((choice) => {
          const selected = value === choice.value;
          return (
            <label
              key={choice.value}
              className={[
                'flex min-h-tap cursor-pointer items-center gap-3 rounded-tile border px-4 py-3 font-body',
                'transition-colors focus-within:ring-2 focus-within:ring-primary/40',
                selected
                  ? 'border-primary bg-primary/5 text-text-strong'
                  : 'border-border bg-surface-card text-text-body hover:border-primary/40',
              ].join(' ')}
            >
              <input
                type="radio"
                name={`question-${question.id}`}
                value={choice.value}
                checked={selected}
                onChange={() => onChange(choice.value)}
                className="size-4 accent-primary"
              />
              <span>{choice.label}</span>
            </label>
          );
        })}
      </div>
    </fieldset>
  );
}
