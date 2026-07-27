import type { ReactNode } from 'react';
import { useParams } from 'react-router';
import { Button } from '@/components/ui';
import { AttemptAnswerReview } from './AttemptAnswerReview';
import { useAttemptResult } from './useAttemptResult';
import type { AttemptResult } from './results.schemas';

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

      <ol className="mt-8 flex flex-col gap-5">
        {result.answers.map((answer, index) => (
          <li key={answer.questionId}>
            <AttemptAnswerReview index={index + 1} answer={answer} generating={generating} />
          </li>
        ))}
      </ol>
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
