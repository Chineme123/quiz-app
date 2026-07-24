import { useState, type FormEvent } from 'react';
import { Button, Select, TextField } from '@/components/ui';
import type { QuestionInput } from './authoring.api';
import type { AuthoredQuestion, QuestionType } from './authoring.schemas';

const TYPE_OPTIONS = [
  { value: 'MultipleChoice', label: 'Multiple choice' },
  { value: 'TrueFalse', label: 'True or false' },
  { value: 'ShortAnswer', label: 'Short answer' },
];

interface QuestionFormProps {
  /** The question being edited, or undefined when adding a new one. */
  initial?: AuthoredQuestion;
  submitting: boolean;
  /** A server side reason the last submit was refused, shown above the buttons. */
  error: string | null;
  onSubmit: (input: QuestionInput) => void;
  onCancel: () => void;
}

/**
 * Add or edit one question (spec 0009, AC-3). The fields follow the chosen type, and the same
 * shape is sent for both add and edit. A question's type is fixed once it exists, so editing
 * shows it read only: changing type is a remove then add.
 */
export function QuestionForm({ initial, submitting, error, onSubmit, onCancel }: QuestionFormProps) {
  const editing = initial !== undefined;

  const [type, setType] = useState<QuestionType>(initial?.questionType ?? 'MultipleChoice');
  const [prompt, setPrompt] = useState(initial?.prompt ?? '');
  const [points, setPoints] = useState(String(initial?.points ?? 1));
  const [options, setOptions] = useState<string[]>(
    initial?.options !== null && initial?.options !== undefined && initial.options.length >= 2
      ? initial.options
      : ['', ''],
  );
  const [correctIndex, setCorrectIndex] = useState(String(initial?.correctOptionIndex ?? 0));
  const [correctBool, setCorrectBool] = useState(String(initial?.correctAnswerBool ?? true));
  const [correctText, setCorrectText] = useState(initial?.correctAnswerText ?? '');

  function setOption(index: number, value: string) {
    setOptions((current) => current.map((option, i) => (i === index ? value : option)));
  }

  function addOption() {
    setOptions((current) => [...current, '']);
  }

  function removeOption(index: number) {
    setOptions((current) => {
      const next = current.filter((_, i) => i !== index);
      // Keep the correct answer pointing at something that still exists.
      if (Number(correctIndex) >= next.length) setCorrectIndex(String(Math.max(0, next.length - 1)));
      return next;
    });
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const base = { questionType: type, prompt: prompt.trim(), points: Number(points) };
    if (type === 'MultipleChoice') {
      onSubmit({
        ...base,
        options: options.map((option) => option.trim()),
        correctOptionIndex: Number(correctIndex),
      });
      return;
    }
    if (type === 'TrueFalse') {
      onSubmit({ ...base, correctAnswerBool: correctBool === 'true' });
      return;
    }
    onSubmit({ ...base, correctAnswerText: correctText.trim() });
  }

  return (
    <form onSubmit={handleSubmit} noValidate className="flex flex-col gap-4">
      {editing ? (
        <p className="font-body text-sm text-text-muted">
          Type: {TYPE_OPTIONS.find((option) => option.value === type)?.label}. To use a different
          type, remove this question and add a new one.
        </p>
      ) : (
        <Select
          label="Type"
          value={type}
          onChange={(event) => setType(event.target.value as QuestionType)}
          options={TYPE_OPTIONS}
        />
      )}

      {/* A multiline TextField renders a textarea, which takes its native props through
          textareaProps; value/onChange passed at the top level would be silently dropped. */}
      <TextField
        label="Question"
        required
        multiline
        rows={3}
        textareaProps={{ value: prompt, onChange: (event) => setPrompt(event.target.value) }}
      />

      <TextField
        label="Points"
        type="number"
        min={1}
        required
        value={points}
        onChange={(event) => setPoints(event.target.value)}
      />

      {type === 'MultipleChoice' && (
        <fieldset className="flex flex-col gap-3">
          <legend className="font-body text-sm text-text-body">Answer choices</legend>
          {options.map((option, index) => (
            <div key={index} className="flex items-end gap-2">
              <div className="grow">
                <TextField
                  label={`Choice ${index + 1}`}
                  value={option}
                  onChange={(event) => setOption(index, event.target.value)}
                />
              </div>
              {options.length > 2 && (
                <Button
                  variant="ghost"
                  onClick={() => removeOption(index)}
                  aria-label={`Remove choice ${index + 1}`}
                >
                  Remove
                </Button>
              )}
            </div>
          ))}
          <div>
            <Button variant="subtle" onClick={addOption}>
              Add a choice
            </Button>
          </div>
          <Select
            label="Which one is right?"
            value={correctIndex}
            onChange={(event) => setCorrectIndex(event.target.value)}
            options={options.map((option, index) => ({
              value: String(index),
              label: option.trim() === '' ? `Choice ${index + 1}` : option,
            }))}
          />
        </fieldset>
      )}

      {type === 'TrueFalse' && (
        <Select
          label="Which one is right?"
          value={correctBool}
          onChange={(event) => setCorrectBool(event.target.value)}
          options={[
            { value: 'true', label: 'True' },
            { value: 'false', label: 'False' },
          ]}
        />
      )}

      {type === 'ShortAnswer' && (
        <TextField
          label="Correct answer"
          required
          value={correctText}
          onChange={(event) => setCorrectText(event.target.value)}
          hint="Students' answers are matched to this, ignoring case and spacing."
        />
      )}

      {error !== null && (
        <p role="alert" className="font-body text-sm text-danger">
          {error}
        </p>
      )}

      <div className="flex gap-2">
        <Button variant="secondary" onClick={onCancel}>
          Cancel
        </Button>
        <Button type="submit" loading={submitting}>
          {editing ? 'Save question' : 'Add question'}
        </Button>
      </div>
    </form>
  );
}
