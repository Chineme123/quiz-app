import type { ReactNode } from 'react';
import { useParams } from 'react-router';
import { Button, Card } from '@/components/ui';
import { useAttemptResult } from './useAttemptResult';
import type { AnswerResult, AttemptResult } from './results.schemas';

/**
 * The student results screen (spec 0005, AC-12). The score and per question breakdown
 * appear as soon as the attempt is graded; while feedback is still being written the
 * feedback areas show a calm generating state and the query polls until it is Ready.
 * AI and deterministic feedback are shown the same way, in Quiztin's supportive voice.
 */
export function ResultsPage() {
  const { attemptId } = useParams<{ attemptId: string }>();
  const query = useAttemptResult(attemptId ?? '');

  if (!attemptId || query.data === null) {
    return (
      <ResultsState>
        We couldn&rsquo;t find that result. It may not exist, or it isn&rsquo;t yours to view.
      </ResultsState>
    );
  }
  if (query.isPending) {
    return <ResultsState>Loading your results…</ResultsState>;
  }
  if (query.isError) {
    return (
      <ResultsState>
        <p className="text-text-body">We couldn&rsquo;t load your results.</p>
        <Button className="mt-4" onClick={() => void query.refetch()}>
          Try again
        </Button>
      </ResultsState>
    );
  }

  return <Results result={query.data} />;
}

function Results({ result }: { result: AttemptResult }) {
  const total = result.answers.length;
  const correct = result.answers.filter((answer) => answer.isCorrect).length;
  const generating = result.feedbackStatus === 'Pending';

  return (
    <div className="mx-auto max-w-reading">
      <header>
        <h1 className="font-display text-3xl text-text-strong">{headline(correct, total)}</h1>
        <p className="mt-2 text-text-body">
          You got{' '}
          <strong className="text-text-strong">
            {correct} of {total}
          </strong>{' '}
          right.{generating ? ' Your feedback is on its way.' : ''}
        </p>
      </header>

      <ol className="mt-8 flex list-none flex-col gap-5">
        {result.answers.map((answer, index) => (
          <li key={answer.questionId}>
            <AnswerReview index={index + 1} answer={answer} generating={generating} />
          </li>
        ))}
      </ol>
    </div>
  );
}

function AnswerReview({
  index,
  answer,
  generating,
}: {
  index: number;
  answer: AnswerResult;
  generating: boolean;
}) {
  const correct = answer.isCorrect;
  return (
    <Card padding="lg" className={correct ? 'border-l-4 border-l-success' : 'border-l-4 border-l-danger'}>
      <div className="flex items-center justify-between gap-3">
        <span className="font-body text-sm text-text-muted">Question {index}</span>
        <StatusPill correct={correct} />
      </div>

      <p className="mt-2 font-display text-lg text-text-strong">{answer.questionText}</p>

      <dl className="mt-3 flex flex-col gap-1 text-sm">
        <div className="flex flex-wrap gap-x-2">
          <dt className="text-text-muted">Your answer:</dt>
          <dd className={`font-semibold ${correct ? 'text-success-text' : 'text-danger-text'}`}>
            {answer.providedAnswer}
          </dd>
        </div>
        {!correct && (
          <div className="flex flex-wrap gap-x-2">
            <dt className="text-text-muted">Correct answer:</dt>
            <dd className="font-semibold text-text-strong">{answer.correctAnswer}</dd>
          </div>
        )}
      </dl>

      <FeedbackBlock feedback={answer.feedback ?? null} generating={generating} />
    </Card>
  );
}

function StatusPill({ correct }: { correct: boolean }) {
  const classes = correct
    ? 'bg-success-soft text-success-text'
    : 'bg-danger-soft text-danger-text';
  return (
    <span className={`rounded-full px-3 py-1 text-xs font-bold ${classes}`}>
      {correct ? 'Correct' : 'To review'}
    </span>
  );
}

/** Quiztin's AI voice. AI and deterministic feedback render identically (AC-12). The
 *  live region announces the change from the generating state to the written feedback. */
function FeedbackBlock({ feedback, generating }: { feedback: string | null; generating: boolean }) {
  const waiting = generating || feedback === null;
  return (
    <div
      className="mt-4 rounded-[var(--radius-tile)] border border-ai-border bg-ai-surface p-4"
      aria-live="polite"
    >
      <div className="flex items-center gap-2">
        <span
          className="grid size-6 place-items-center rounded-full bg-accent text-xs font-bold text-text-on-accent"
          aria-hidden="true"
        >
          Q
        </span>
        <span className="text-xs font-bold uppercase tracking-wide text-ai-accent">Quiztin</span>
      </div>
      {waiting ? (
        <p className="mt-2 text-sm text-text-muted">Quiztin is writing your feedback…</p>
      ) : (
        <p className="mt-2 text-sm text-text-body">{feedback}</p>
      )}
    </div>
  );
}

function ResultsState({ children }: { children: ReactNode }) {
  return (
    <div className="mx-auto max-w-reading">
      <h1 className="font-display text-3xl text-text-strong">Your results</h1>
      <div className="mt-8 text-text-muted">{children}</div>
    </div>
  );
}

/** Warm, encouraging, and honest. Misses are framed as something to review, never a failure. */
function headline(correct: number, total: number): string {
  if (total === 0) return 'Your results';
  if (correct === total) return 'Brilliant — every one right.';
  if (correct === 0) return 'A tricky one — let’s review it together.';
  if (correct / total >= 0.6) return 'Nicely done.';
  return 'Good effort — a few to review.';
}
