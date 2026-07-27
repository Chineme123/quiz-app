import { Link, useParams } from 'react-router';
import { Button, Card } from '@/components/ui';
import { useClassroomResults } from './useClassroomResultsQueries';
import { percentLabel, scoreLabel } from './resultsFormat';
import type { QuizResultSummary } from './classroomResults.schemas';

/**
 * How a class is doing, at a glance (spec 0010, AC-2): each published or attempted quiz with how
 * many students have finished and the class average. Owner scoped, so a class that is not yours
 * reads as missing (AC-1, AC-12). Served for archived classes too (AC-9).
 */
export function ClassroomResultsPage() {
  const { classroomId = '' } = useParams<{ classroomId: string }>();
  const query = useClassroomResults(classroomId);

  if (query.isPending) {
    return (
      <main className="mx-auto w-full max-w-3xl px-4 py-8">
        <p className="font-body text-text-muted">Loading results…</p>
      </main>
    );
  }

  if (query.isError) {
    return (
      <main className="mx-auto w-full max-w-3xl px-4 py-8">
        <Card padding="lg">
          <p className="font-body text-text-body">We couldn&rsquo;t load these results.</p>
          <Button className="mt-4" onClick={() => void query.refetch()}>
            Try again
          </Button>
        </Card>
      </main>
    );
  }

  // Null means the class is not yours, or there is no such class: the same answer either way (AC-12).
  if (query.data === null) {
    return (
      <main className="mx-auto w-full max-w-3xl px-4 py-8">
        <Card padding="lg">
          <h1 className="font-display text-2xl text-text-strong">We couldn&rsquo;t find that class</h1>
          <p className="mt-2 font-body text-text-muted">
            It may have been removed, or it isn&rsquo;t one of yours.
          </p>
          <Link to="/dashboard" className="mt-4 inline-block font-body text-text-link">
            Back to your dashboard
          </Link>
        </Card>
      </main>
    );
  }

  const results = query.data;

  return (
    <main className="mx-auto w-full max-w-3xl px-4 py-8">
      <header className="mb-6">
        <p className="font-body text-sm uppercase tracking-wide text-text-muted">Results</p>
        <h1 className="font-display text-2xl text-text-strong">{results.classroomName}</h1>
        <p className="mt-1 font-body text-text-muted">
          {results.studentCount} {results.studentCount === 1 ? 'student' : 'students'}
          {results.isArchived && ' · archived'}
        </p>
        <div className="mt-2 flex flex-wrap gap-x-4 gap-y-1">
          <Link to={`/classrooms/${classroomId}`} className="inline-block font-body text-sm text-text-link">
            Back to the class
          </Link>
          {results.quizzes.length > 0 && (
            <Link
              to={`/classrooms/${classroomId}/results/students`}
              className="inline-block font-body text-sm text-text-link"
            >
              See each student&rsquo;s standing
            </Link>
          )}
        </div>
      </header>

      {results.quizzes.length === 0 ? (
        <Card padding="lg">
          <p className="font-body text-text-body">
            No results to show yet. Once you publish a quiz, or a student takes one, it&rsquo;ll appear
            here.
          </p>
        </Card>
      ) : (
        <ul className="flex flex-col gap-3">
          {results.quizzes.map((quiz) => (
            <li key={quiz.quizId}>
              <QuizSummaryCard classroomId={classroomId} quiz={quiz} studentCount={results.studentCount} />
            </li>
          ))}
        </ul>
      )}
    </main>
  );
}

function QuizSummaryCard({
  classroomId,
  quiz,
  studentCount,
}: {
  classroomId: string;
  quiz: QuizResultSummary;
  studentCount: number;
}) {
  return (
    <Card padding="lg">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <Link
            to={`/quizzes/${quiz.quizId}/results`}
            state={{ classroomId }}
            className="font-display text-lg text-text-strong underline-offset-2 hover:underline"
          >
            {quiz.title}
          </Link>
          <p className="mt-1 font-body text-sm text-text-muted">
            {quiz.isPublished ? 'Published' : 'Draft'} · {quiz.completionCount} of {studentCount} finished
          </p>
        </div>
        <div className="text-right">
          {quiz.completionCount === 0 ? (
            <p className="font-body text-sm text-text-muted">No one has finished yet</p>
          ) : (
            <>
              <p className="font-display text-lg text-text-strong">
                {quiz.averagePercent === null ? '' : percentLabel(quiz.averagePercent)}
              </p>
              <p className="font-body text-sm text-text-muted">
                class average
                {quiz.averageScore === null ? '' : ` · ${scoreLabel(quiz.averageScore, quiz.totalPoints)}`}
              </p>
            </>
          )}
        </div>
      </div>
    </Card>
  );
}
