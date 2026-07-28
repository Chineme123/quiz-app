import { Link } from 'react-router';
import { Button, Card } from '@/components/ui';
import { useMyResults } from './useMyResults';
import { percentLabel, scoreLabel, dateLabel } from './resultsFormat';
import type { MyResultsClassroom, MyResultsQuiz } from './myResults.schemas';

/**
 * Your results in one place (spec 0011): every quiz you have finished, grouped by class, with how
 * you are standing in each. Each row opens its per question detail, the same screen shown right
 * after taking a quiz (AC-5). Calm and encouraging throughout (ui-rules §1): a class you have not
 * finished anything in is not hidden, it says so gently (AC-2), and finishing nothing anywhere is a
 * warm empty state, not an error (AC-8).
 */
export function MyResultsPage() {
  const query = useMyResults();

  if (query.isPending) {
    return (
      <main className="mx-auto w-full max-w-3xl px-4 py-8">
        <p className="font-body text-text-muted">Loading your results…</p>
      </main>
    );
  }

  if (query.isError) {
    return (
      <main className="mx-auto w-full max-w-3xl px-4 py-8">
        <Card padding="lg">
          <p className="font-body text-text-body">We couldn&rsquo;t load your results just now.</p>
          <Button className="mt-4" onClick={() => void query.refetch()}>
            Try again
          </Button>
        </Card>
      </main>
    );
  }

  const { classrooms } = query.data;

  return (
    <main className="mx-auto w-full max-w-3xl px-4 py-8">
      <header className="mb-6">
        <p className="font-body text-sm uppercase tracking-wide text-text-muted">Your progress</p>
        <h1 className="font-display text-2xl text-text-strong">My results</h1>
        <p className="mt-1 font-body text-text-muted">
          Every quiz you&rsquo;ve finished, and how you&rsquo;re doing in each class.
        </p>
      </header>

      {classrooms.length === 0 ? (
        <Card padding="lg">
          <p className="font-body text-text-body">
            You haven&rsquo;t finished a quiz yet. When you do, your results will gather here.
          </p>
          <Link to="/quizzes" className="mt-4 inline-block font-body text-text-link">
            See what&rsquo;s available to take
          </Link>
        </Card>
      ) : (
        <div className="flex flex-col gap-8">
          {classrooms.map((classroom) => (
            <ClassroomResultsGroup key={classroom.classroomId} classroom={classroom} />
          ))}
        </div>
      )}
    </main>
  );
}

function ClassroomResultsGroup({ classroom }: { classroom: MyResultsClassroom }) {
  const finishedCount = classroom.quizzes.length;

  return (
    <section>
      <header className="mb-3 flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1">
        <h2 className="font-display text-lg text-text-strong">
          {classroom.classroomName}
          {classroom.isArchived && (
            <span className="ml-2 font-body text-sm font-normal text-text-muted">· archived</span>
          )}
        </h2>
        {classroom.standingPercent !== null && (
          <p className="font-body text-sm text-text-muted">
            You&rsquo;re standing at{' '}
            <span className="font-display text-text-strong">{percentLabel(classroom.standingPercent)}</span>{' '}
            across {finishedCount} {finishedCount === 1 ? 'quiz' : 'quizzes'}
          </p>
        )}
      </header>

      {finishedCount === 0 ? (
        <Card padding="lg">
          <p className="font-body text-text-muted">
            Nothing finished here yet. Take a quiz in this class and it&rsquo;ll show up.
          </p>
        </Card>
      ) : (
        <ul className="flex flex-col gap-3">
          {classroom.quizzes.map((quiz) => (
            <li key={quiz.quizId}>
              <QuizResultCard quiz={quiz} />
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}

function QuizResultCard({ quiz }: { quiz: MyResultsQuiz }) {
  return (
    <Card padding="lg">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="min-w-0">
          <Link
            to={`/results/${quiz.attemptId}`}
            className="font-display text-lg text-text-strong underline-offset-2 hover:underline"
          >
            {quiz.title}
          </Link>
          <p className="mt-1 font-body text-sm text-text-muted">Finished {dateLabel(quiz.submittedAt)}</p>
        </div>
        <div className="text-right">
          {quiz.percent !== null ? (
            <>
              <p className="font-display text-lg text-text-strong">{percentLabel(quiz.percent)}</p>
              {quiz.score !== null && (
                <p className="font-body text-sm text-text-muted">
                  {scoreLabel(quiz.score, quiz.totalPoints)}
                </p>
              )}
            </>
          ) : (
            <p className="font-display text-lg text-text-strong">
              {quiz.score !== null ? scoreLabel(quiz.score, quiz.totalPoints) : '—'}
            </p>
          )}
        </div>
      </div>
    </Card>
  );
}
