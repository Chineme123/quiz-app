import { Link, useLocation, useParams } from 'react-router';
import { Button } from '@/components/ui';
import { AttemptAnswerReview } from '@/features/results/AttemptAnswerReview';
import type { AttemptResult } from '@/features/results/results.schemas';
import { useStudentAttempt } from './useClassroomResultsQueries';

/** Optional context handed over from the results list, so the header can name the student and quiz
 *  without another request. Absent when the page is reached by a direct link. */
type DrillDownState = { displayName?: string; quizTitle?: string };

/**
 * The teacher's drill-down into one student's latest submitted attempt (spec 0010, AC-6): the same
 * per-question breakdown the student sees on their own results screen, reached through quiz
 * ownership. Owner scoped, so a quiz that is not yours, or a student with no submitted attempt,
 * reads as missing (AC-1). Misses are framed to review, never as a failure (ui-rules §1).
 */
export function StudentAttemptPage() {
  const { quizId = '', studentId = '' } = useParams<{ quizId: string; studentId: string }>();
  const location = useLocation();
  const context = (location.state ?? {}) as DrillDownState;
  const query = useStudentAttempt(quizId, studentId);

  const who = context.displayName ?? 'This student';

  if (query.isPending) {
    return (
      <main className="mx-auto w-full max-w-reading px-4 py-8">
        <p className="font-body text-text-muted">Loading the attempt…</p>
      </main>
    );
  }

  if (query.isError) {
    return (
      <main className="mx-auto w-full max-w-reading px-4 py-8">
        <p className="font-body text-text-body">We couldn&rsquo;t load this attempt.</p>
        <Button className="mt-4" onClick={() => void query.refetch()}>
          Try again
        </Button>
      </main>
    );
  }

  // Null covers "not your quiz" and "this student has no submitted attempt" alike (AC-1).
  if (query.data === null) {
    return (
      <main className="mx-auto w-full max-w-reading px-4 py-8">
        <h1 className="font-display text-2xl text-text-strong">We couldn&rsquo;t find that attempt</h1>
        <p className="mt-2 font-body text-text-muted">
          It may not exist, this student may not have finished the quiz, or it isn&rsquo;t one of
          yours.
        </p>
        <Link to={`/quizzes/${quizId}/results`} className="mt-4 inline-block font-body text-text-link">
          Back to quiz results
        </Link>
      </main>
    );
  }

  return <Attempt result={query.data} who={who} quizTitle={context.quizTitle} quizId={quizId} />;
}

function Attempt({
  result,
  who,
  quizTitle,
  quizId,
}: {
  result: AttemptResult;
  who: string;
  quizTitle?: string;
  quizId: string;
}) {
  const total = result.answers.length;
  const correct = result.answers.filter((answer) => answer.isCorrect).length;
  const generating = result.feedbackStatus === 'Pending';

  return (
    <div className="mx-auto max-w-reading px-4 py-8">
      <header>
        <p className="font-body text-sm uppercase tracking-wide text-text-muted">Attempt</p>
        <h1 className="font-display text-2xl text-text-strong">
          {who}
          {quizTitle ? ` · ${quizTitle}` : ''}
        </h1>
        <p className="mt-2 font-body text-text-body">
          {who} got{' '}
          <strong className="text-text-strong">
            {correct} of {total}
          </strong>{' '}
          right.{generating ? ' Feedback is still being written.' : ''}
        </p>
        <Link to={`/quizzes/${quizId}/results`} className="mt-2 inline-block font-body text-sm text-text-link">
          Back to quiz results
        </Link>
      </header>

      <ol className="mt-8 flex flex-col gap-5">
        {result.answers.map((answer, index) => (
          <li key={answer.questionId}>
            <AttemptAnswerReview index={index + 1} answer={answer} generating={generating} viewer="teacher" />
          </li>
        ))}
      </ol>
    </div>
  );
}
