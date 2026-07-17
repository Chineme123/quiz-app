import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate, useParams } from 'react-router';
import { Button, Card } from '@/components/ui';
import { useAttemptQuestions } from './useTakeQueries';
import { useDraftAutosave } from './useDraftAutosave';
import { formatCountdown, useCountdown } from './useCountdown';
import { QuestionInput } from './QuestionInput';
import { QuestionNavigator } from './QuestionNavigator';
import { submitAttempt } from './take.api';
import type { SaveState } from './useDraftAutosave';

/** The quiet, honest word for whether the student's work is safe (spec 0006, AC-8). */
function SaveStatus({ state }: { state: SaveState }) {
  const label: Record<SaveState, string> = {
    idle: '',
    saving: 'Saving…',
    saved: 'Saved',
    retrying: "Couldn't save — trying again",
    failed: "Couldn't save",
    closed: 'No longer saving',
  };
  const text = label[state];
  if (!text) return null;

  return (
    // Polite, so it never interrupts a student mid question, but a screen reader still hears it.
    <span
      aria-live="polite"
      className={`font-body text-sm ${state === 'retrying' || state === 'failed' ? 'text-danger' : 'text-text-muted'}`}
    >
      {text}
    </span>
  );
}

/**
 * The take screen (spec 0006). One question at a time with a navigator, a countdown that runs on
 * the server's clock, and answers saved as they are picked, so a refresh or a dead battery never
 * costs a student their only attempt. Submitting sends just an idempotency key: the answers on
 * the server are what gets graded.
 */
export function TakeQuizPage() {
  const { attemptId = '' } = useParams<{ attemptId: string }>();
  const navigate = useNavigate();
  const { data, isPending, isError, refetch } = useAttemptQuestions(attemptId);

  const [answers, setAnswers] = useState<Record<string, string>>({});
  const [currentIndex, setCurrentIndex] = useState(0);
  const [confirming, setConfirming] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const hydrated = useRef(false);

  const running = data?.status === 'InProgress';
  const { saveState, save, flushNow } = useDraftAutosave(attemptId, running === true);
  const secondsLeft = useCountdown(data?.expiresAt, data?.serverNow);

  // Restore the work already saved, once (AC-9). Guarded, or a later render would clobber what
  // the student has typed since with the set the server had at load.
  useEffect(() => {
    if (data && !hydrated.current) {
      setAnswers(data.draftAnswers);
      hydrated.current = true;
    }
  }, [data]);

  // One idempotency key per attempt, minted once. Reused on every retry so a network hiccup
  // during submit can never grade twice.
  const commandId = useMemo(() => crypto.randomUUID(), []);

  // Memoised, or `?? []` would hand back a new array every render and recompute the set below
  // on each one.
  const questions = useMemo(() => data?.questions ?? [], [data]);
  const answeredIndexes = useMemo(
    () => new Set(questions.map((q, i) => (answers[q.id]?.trim() ? i : -1)).filter((i) => i >= 0)),
    [questions, answers],
  );
  const unansweredCount = questions.length - answeredIndexes.size;

  const doSubmit = useCallback(async () => {
    setSubmitting(true);
    setSubmitError(null);
    try {
      // Land any debounced answer before grading, so the last thing they picked counts.
      await flushNow();
      await submitAttempt(attemptId, commandId);
      await navigate(`/results/${attemptId}`);
    } catch {
      setSubmitError("We couldn't submit that just now. Your answers are saved — try again.");
      setSubmitting(false);
      setConfirming(false);
    }
  }, [attemptId, commandId, flushNow, navigate]);

  // Time is up: submit what is saved rather than letting the work sit there (AC-10). The server
  // grades the drafts on a late submit too, so a slow network here costs nothing.
  // Deferred a tick rather than called straight from the effect body, so submitting does not set
  // state synchronously during the effect and cascade a re-render.
  useEffect(() => {
    if (secondsLeft !== 0 || !running || submitting) return;
    const id = window.setTimeout(() => void doSubmit(), 0);
    return () => window.clearTimeout(id);
  }, [secondsLeft, running, submitting, doSubmit]);

  function updateAnswer(questionId: string, value: string) {
    const next = { ...answers, [questionId]: value };
    setAnswers(next);
    save(next); // whole set, never a delta
  }

  if (isPending) return <p className="p-8 font-body text-text-muted">Loading your quiz…</p>;

  if (isError) {
    return (
      <main className="mx-auto max-w-lg p-8">
        <Card padding="lg">
          <p className="font-body text-text-body">We couldn't load that quiz.</p>
          <Button className="mt-4" onClick={() => void refetch()}>
            Try again
          </Button>
        </Card>
      </main>
    );
  }

  // Null means it does not exist or is not theirs — the server answers the same either way, and
  // so do we, so this screen never reveals that someone else's attempt exists (AC-5).
  if (!data) {
    return (
      <main className="mx-auto max-w-lg p-8">
        <Card padding="lg">
          <h1 className="font-display text-xl text-text-strong">We couldn't find that quiz</h1>
          <p className="mt-2 font-body text-text-body">
            It may have finished, or the link may be wrong.
          </p>
          <Button className="mt-4" onClick={() => void navigate('/quizzes')}>
            Back to your quizzes
          </Button>
        </Card>
      </main>
    );
  }

  if (!running) {
    return (
      <main className="mx-auto max-w-lg p-8">
        <Card padding="lg">
          <h1 className="font-display text-xl text-text-strong">This attempt is finished</h1>
          <p className="mt-2 font-body text-text-body">You can look back over how it went.</p>
          <Button className="mt-4" onClick={() => void navigate(`/results/${attemptId}`)}>
            See your result
          </Button>
        </Card>
      </main>
    );
  }

  const question = questions[currentIndex];

  return (
    <div className="min-h-full">
      <header className="sticky top-0 z-10 border-b border-border bg-surface-card/95 backdrop-blur">
        <div className="mx-auto flex max-w-5xl flex-wrap items-center justify-between gap-3 px-4 py-3">
          <h1 className="font-display text-lg text-text-strong">{data.quizTitle}</h1>
          <div className="flex items-center gap-4">
            <SaveStatus state={saveState} />
            {secondsLeft !== null && (
              <span
                aria-live="off"
                aria-label={`${Math.floor(secondsLeft / 60)} minutes ${secondsLeft % 60} seconds remaining`}
                className={`font-mono text-lg tabular-nums ${secondsLeft <= 60 ? 'text-danger' : 'text-text-strong'}`}
              >
                {formatCountdown(secondsLeft)}
              </span>
            )}
          </div>
        </div>
      </header>

      <main className="mx-auto grid max-w-5xl gap-6 px-4 py-6 lg:grid-cols-[1fr_auto]">
        <div className="min-w-0">
          {question && (
            <Card padding="lg">
              <p className="font-body text-sm text-text-muted">
                Question {currentIndex + 1} of {questions.length} · {question.points}{' '}
                {question.points === 1 ? 'point' : 'points'}
              </p>
              <h2 id="question-prompt" className="mt-2 font-display text-xl text-text-strong">
                {question.prompt}
              </h2>
              <div className="mt-5">
                <QuestionInput
                  question={question}
                  value={answers[question.id] ?? ''}
                  onChange={(value) => updateAnswer(question.id, value)}
                  disabled={submitting}
                  labelledBy="question-prompt"
                />
              </div>
            </Card>
          )}

          {submitError && (
            <p role="alert" className="mt-4 font-body text-danger">
              {submitError}
            </p>
          )}

          {/* Back and Next only. Submitting lives once, with the navigator, so the page never
              offers two buttons that do the same thing, and it is reachable from any question
              rather than only the last one. */}
          <div className="mt-5 flex items-center justify-between gap-3">
            <Button
              variant="secondary"
              disabled={currentIndex === 0}
              onClick={() => setCurrentIndex((i) => Math.max(0, i - 1))}
            >
              Back
            </Button>
            <Button
              disabled={currentIndex >= questions.length - 1}
              onClick={() => setCurrentIndex((i) => Math.min(questions.length - 1, i + 1))}
            >
              Next
            </Button>
          </div>
        </div>

        <aside className="lg:w-56">
          <Card padding="md">
            <h2 className="mb-3 font-body text-sm text-text-muted">Your progress</h2>
            <QuestionNavigator
              total={questions.length}
              currentIndex={currentIndex}
              answeredIndexes={answeredIndexes}
              onJump={setCurrentIndex}
            />
            <Button className="mt-4" fullWidth onClick={() => setConfirming(true)} loading={submitting}>
              Submit quiz
            </Button>
          </Card>
        </aside>
      </main>

      {confirming && (
        // Warn, never block: a student who genuinely doesn't know an answer must still be able
        // to finish (AC-13).
        <div
          role="dialog"
          aria-modal="true"
          aria-labelledby="confirm-title"
          className="fixed inset-0 z-20 flex items-center justify-center bg-text-strong/40 p-4"
        >
          <Card padding="lg" className="w-full max-w-md">
            <h2 id="confirm-title" className="font-display text-xl text-text-strong">
              Ready to submit?
            </h2>
            <p className="mt-2 font-body text-text-body">
              {unansweredCount > 0
                ? `You've left ${unansweredCount} ${unansweredCount === 1 ? 'question' : 'questions'} unanswered. You can still submit.`
                : "You've answered everything. Nice work."}
            </p>
            <div className="mt-5 flex justify-end gap-3">
              <Button variant="secondary" onClick={() => setConfirming(false)} disabled={submitting}>
                Keep working
              </Button>
              <Button onClick={() => void doSubmit()} loading={submitting}>
                Submit
              </Button>
            </div>
          </Card>
        </div>
      )}
    </div>
  );
}
