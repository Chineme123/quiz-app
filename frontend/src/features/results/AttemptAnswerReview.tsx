import { Card } from '@/components/ui';
import type { AnswerResult } from './results.schemas';

/** Whose screen this renders on: the student viewing their own result (spec 0005) or the teacher
 *  drilling into a student's attempt (spec 0010, AC-6). Only the wording differs. */
export type AttemptViewer = 'student' | 'teacher';

/**
 * One question's review: the answer given, the correct answer when it was missed, and Quiztin's
 * feedback. Shared by the student's own results screen and the teacher's drill-down, so both show
 * exactly the same breakdown. Misses are framed as "to review", never "wrong" (ui-rules §1).
 */
export function AttemptAnswerReview({
  index,
  answer,
  generating,
  viewer = 'student',
}: {
  index: number;
  answer: AnswerResult;
  generating: boolean;
  viewer?: AttemptViewer;
}) {
  const correct = answer.isCorrect;
  const answerLabel = viewer === 'teacher' ? 'Answer:' : 'Your answer:';
  return (
    <Card padding="lg" className={correct ? 'border-l-4 border-l-success' : 'border-l-4 border-l-danger'}>
      <div className="flex items-center justify-between gap-3">
        <span className="font-body text-sm text-text-muted">Question {index}</span>
        <StatusPill correct={correct} />
      </div>

      <p className="mt-2 font-display text-lg text-text-strong">{answer.questionText}</p>

      <dl className="mt-3 flex flex-col gap-1 text-sm">
        <div className="flex flex-wrap gap-x-2">
          <dt className="text-text-muted">{answerLabel}</dt>
          <dd className={`font-semibold ${correct ? 'text-success-text' : 'text-danger-text'}`}>
            {/* A skipped question now has a graded row with a blank answer (spec 0006), so say
                "Not answered" rather than render an empty value that reads like a bug. */}
            {answer.providedAnswer || 'Not answered'}
          </dd>
        </div>
        {!correct && (
          <div className="flex flex-wrap gap-x-2">
            <dt className="text-text-muted">Correct answer:</dt>
            <dd className="font-semibold text-text-strong">{answer.correctAnswer}</dd>
          </div>
        )}
      </dl>

      <FeedbackBlock feedback={answer.feedback ?? null} generating={generating} viewer={viewer} />
    </Card>
  );
}

function StatusPill({ correct }: { correct: boolean }) {
  const classes = correct ? 'bg-success-soft text-success-text' : 'bg-danger-soft text-danger-text';
  return (
    <span className={`rounded-full px-3 py-1 text-xs font-bold ${classes}`}>
      {correct ? 'Correct' : 'To review'}
    </span>
  );
}

/** Quiztin's AI voice. AI and deterministic feedback render identically (AC-12). The live region
 *  announces the change from the generating state to the written feedback. */
function FeedbackBlock({
  feedback,
  generating,
  viewer,
}: {
  feedback: string | null;
  generating: boolean;
  viewer: AttemptViewer;
}) {
  const waiting = generating || feedback === null;
  const waitingText =
    viewer === 'teacher' ? 'Quiztin is writing the feedback…' : 'Quiztin is writing your feedback…';
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
        <p className="mt-2 text-sm text-text-muted">{waitingText}</p>
      ) : (
        <p className="mt-2 text-sm text-text-body">{feedback}</p>
      )}
    </div>
  );
}
