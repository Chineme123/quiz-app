import { useState, type ChangeEvent, type FormEvent } from 'react';
import { Button, Card, Dialog, Select, TextField, useToast } from '@/components/ui';
import { toUserMessage } from '@/lib/api/errorMessage';
import { TYPE_LABEL, answerSummary, pointsLabel } from './questionDisplay';
import {
  useAcceptDrafts,
  useDiscardDrafts,
  useDrafts,
  useGenerateQuestions,
} from './useAuthoringQueries';

const DIFFICULTY_OPTIONS = [
  { value: 'easy', label: 'Easy' },
  { value: 'medium', label: 'Medium' },
  { value: 'hard', label: 'Hard' },
];

/** Matches the server's cap, so an oversized file is refused here with a sentence instead of a 413. */
const MAX_FILE_BYTES = 5 * 1024 * 1024;
const ACCEPTED_FILES = '.pdf,.docx';

interface GenerationSectionProps {
  quizId: string;
  classroomId: string;
}

/**
 * Generate a batch of questions with Quiztin, then review it (spec 0009, AC-4, AC-7, AC-8).
 *
 * Nothing is added to the quiz until the teacher says so: generating parks the candidates in a
 * pending batch, and this is where they pick which ones are worth keeping. There is one batch per
 * quiz, so generating again replaces what was waiting, and accepting clears the batch either way.
 *
 * The editor does not render this once a student has an attempt: the question set is fixed then.
 */
export function GenerationSection({ quizId, classroomId }: GenerationSectionProps) {
  const toast = useToast();

  const draftsQuery = useDrafts(quizId);
  const generate = useGenerateQuestions(quizId);
  const accept = useAcceptDrafts(quizId, classroomId);
  const discard = useDiscardDrafts(quizId);

  const [topic, setTopic] = useState('');
  const [difficulty, setDifficulty] = useState('medium');
  const [count, setCount] = useState('5');
  const [sourceText, setSourceText] = useState('');
  const [file, setFile] = useState<File | null>(null);
  const [error, setError] = useState<string | null>(null);

  // Everything is kept by default: the teacher asked for these. Deselecting is the exception.
  const [excluded, setExcluded] = useState<string[]>([]);
  const [confirmingDiscard, setConfirmingDiscard] = useState(false);

  const candidates = draftsQuery.data?.candidates ?? [];
  const chosenIds = candidates.filter((c) => !excluded.includes(c.id)).map((c) => c.id);

  function toggle(id: string) {
    setExcluded((current) =>
      current.includes(id) ? current.filter((x) => x !== id) : [...current, id],
    );
  }

  function handleFile(event: ChangeEvent<HTMLInputElement>) {
    const picked = event.target.files?.[0] ?? null;
    if (picked !== null && picked.size > MAX_FILE_BYTES) {
      setError('That file is over 5 MB. Try a smaller one, or paste the part you want questions on.');
      setFile(null);
      return;
    }
    setError(null);
    setFile(picked);
  }

  function handleGenerate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    const requested = Number(count);
    generate.mutate(
      {
        topic: topic.trim(),
        difficulty,
        count: Number.isFinite(requested) ? requested : 5,
        sourceText: sourceText.trim(),
        file,
      },
      {
        onSuccess: (draft) => {
          setExcluded([]);
          if (draft.candidates.length === 0) {
            toast.show({
              tone: 'info',
              message: "Quiztin couldn't draft anything usable from that. Try a clearer topic.",
            });
            return;
          }
          toast.show({
            tone: 'success',
            message: `${draft.candidates.length} to look over. Keep the ones you like.`,
          });
        },
        onError: (cause) => {
          setError(
            toUserMessage(
              cause,
              "We couldn't draft questions just now. Your quiz is unchanged, so do try again.",
            ),
          );
        },
      },
    );
  }

  function handleAccept() {
    accept.mutate(chosenIds, {
      onSuccess: () => {
        toast.show({
          tone: 'success',
          message: `Added ${String(chosenIds.length)}. Edit any of them like any other question.`,
        });
      },
      onError: (cause) => {
        setError(toUserMessage(cause, "We couldn't add those to your quiz."));
      },
    });
  }

  function handleDiscard() {
    discard.mutate(undefined, {
      onSuccess: () => {
        setConfirmingDiscard(false);
        toast.show({ tone: 'info', message: 'Cleared. Nothing was added to your quiz.' });
      },
      onError: (cause) => {
        setConfirmingDiscard(false);
        toast.show({ tone: 'danger', message: toUserMessage(cause, "We couldn't clear those.") });
      },
    });
  }

  const reviewing = candidates.length > 0;

  return (
    <Card padding="lg" className="mb-6 border-ai-border bg-ai-surface">
      <h2 className="font-display text-lg text-text-strong">
        {reviewing ? 'Look these over' : 'Draft questions with Quiztin'}
      </h2>

      {reviewing ? (
        <>
          <p className="mt-1 max-w-reading font-body text-sm text-ai-text">
            Quiztin drafted these. Nothing is on your quiz yet. Keep the ones you want, then edit
            any of them afterwards like any other question.
          </p>

          <ul className="mt-4 flex flex-col">
            {candidates.map((candidate) => {
              const included = !excluded.includes(candidate.id);
              return (
                <li key={candidate.id} className="border-b border-ai-border py-3 last:border-b-0">
                  <div className="flex flex-wrap items-start justify-between gap-3">
                    <div className="max-w-reading">
                      <p className="font-body text-text-strong">{candidate.prompt}</p>
                      <p className="mt-1 font-body text-sm text-text-muted">
                        {TYPE_LABEL[candidate.questionType]} · {pointsLabel(candidate.points)} ·
                        Answer: {answerSummary(candidate)}
                      </p>
                    </div>
                    <Button
                      variant={included ? 'secondary' : 'ghost'}
                      aria-pressed={included}
                      onClick={() => toggle(candidate.id)}
                    >
                      {included ? 'Keeping' : 'Skipped'}
                    </Button>
                  </div>
                </li>
              );
            })}
          </ul>

          {error !== null && (
            <p role="alert" className="mt-4 font-body text-sm text-danger">
              {error}
            </p>
          )}

          <div className="mt-4 flex flex-wrap items-center gap-2">
            <Button
              variant="accent"
              loading={accept.isPending}
              disabled={chosenIds.length === 0}
              onClick={handleAccept}
            >
              {chosenIds.length === 1 ? 'Add 1 to the quiz' : `Add ${String(chosenIds.length)} to the quiz`}
            </Button>
            <Button variant="ghost" onClick={() => setConfirmingDiscard(true)}>
              Clear these
            </Button>
          </div>
          {chosenIds.length === 0 && (
            <p className="mt-2 font-body text-sm text-text-muted">
              You&rsquo;ve skipped all of them. Keep at least one, or clear the whole batch.
            </p>
          )}
        </>
      ) : (
        <>
          <p className="mt-1 max-w-reading font-body text-sm text-ai-text">
            Say what the questions should cover and Quiztin drafts a set for you to look over.
            Nothing goes on your quiz until you say so.
          </p>

          <form onSubmit={handleGenerate} noValidate className="mt-4 flex flex-col gap-4">
            <TextField
              label="What should they cover?"
              required
              value={topic}
              onChange={(event) => setTopic(event.target.value)}
              hint="A topic or a lesson, for example photosynthesis in year 9 biology."
            />

            <div className="flex flex-wrap gap-4">
              <div className="min-w-40 grow">
                <Select
                  label="How hard"
                  value={difficulty}
                  onChange={(event) => setDifficulty(event.target.value)}
                  options={DIFFICULTY_OPTIONS}
                />
              </div>
              <div className="min-w-40 grow">
                <TextField
                  label="How many"
                  type="number"
                  min={1}
                  max={20}
                  required
                  value={count}
                  onChange={(event) => setCount(event.target.value)}
                />
              </div>
            </div>

            {/* A multiline TextField renders a textarea, whose native props go through
                textareaProps; value/onChange at the top level would be silently dropped. */}
            <TextField
              label="Paste anything they should be based on"
              optional
              multiline
              rows={4}
              hint="Your lesson notes, say. Leave it empty to work from the topic alone."
              textareaProps={{
                value: sourceText,
                onChange: (event) => setSourceText(event.target.value),
              }}
            />

            <div>
              <label htmlFor="source-file" className="font-body text-sm text-text-body">
                Or attach a PDF or Word file
              </label>
              <input
                id="source-file"
                type="file"
                accept={ACCEPTED_FILES}
                onChange={handleFile}
                className="mt-1 block w-full font-body text-sm text-text-body"
              />
              <p className="mt-1 font-body text-sm text-text-muted">
                Up to 5 MB. We read the text out of it to write the questions, and keep nothing.
              </p>
            </div>

            {error !== null && (
              <p role="alert" className="font-body text-sm text-danger">
                {error}
              </p>
            )}

            <div>
              <Button
                type="submit"
                variant="accent"
                loading={generate.isPending}
                disabled={topic.trim() === ''}
              >
                Draft questions
              </Button>
            </div>
            {generate.isPending && (
              <p role="status" className="font-body text-sm text-text-muted">
                Quiztin is writing. This takes a few seconds.
              </p>
            )}
          </form>
        </>
      )}

      <Dialog
        open={confirmingDiscard}
        onClose={() => setConfirmingDiscard(false)}
        title="Clear these drafts?"
        description="They'll be gone. Your quiz is untouched either way, and you can always draft another set."
        tone="danger"
        footer={
          <>
            <Button variant="secondary" onClick={() => setConfirmingDiscard(false)}>
              Keep them
            </Button>
            <Button variant="danger" loading={discard.isPending} onClick={handleDiscard}>
              Clear them
            </Button>
          </>
        }
      />
    </Card>
  );
}
