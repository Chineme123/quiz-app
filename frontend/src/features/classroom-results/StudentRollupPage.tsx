import { useState } from 'react';
import { Link, useParams } from 'react-router';
import { Button, Card } from '@/components/ui';
import { useStudentRollup } from './useClassroomResultsQueries';
import { percentLabel, scoreLabel } from './resultsFormat';
import type { RollupQuizColumn, StudentQuizScore, StudentRollupRow } from './classroomResults.schemas';

/**
 * How each student is doing across the whole class (spec 0010, AC-5): a card per student with
 * their score on each quiz and an overall standing. Standing is the average of each quiz's
 * percentage, so different-sized quizzes compare fairly. Owner scoped and paginated by student
 * (AC-1, AC-10). A card layout, not a wide table, so it reads on a phone too.
 */
export function StudentRollupPage() {
  const { classroomId = '' } = useParams<{ classroomId: string }>();
  const [page, setPage] = useState(1);
  const query = useStudentRollup(classroomId, page);

  if (query.isPending) {
    return (
      <main className="mx-auto w-full max-w-3xl px-4 py-8">
        <p className="font-body text-text-muted">Loading standings…</p>
      </main>
    );
  }

  if (query.isError) {
    return (
      <main className="mx-auto w-full max-w-3xl px-4 py-8">
        <Card padding="lg">
          <p className="font-body text-text-body">We couldn&rsquo;t load these standings.</p>
          <Button className="mt-4" onClick={() => void query.refetch()}>
            Try again
          </Button>
        </Card>
      </main>
    );
  }

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

  const rollup = query.data;
  const pageCount = Math.ceil(rollup.total / rollup.pageSize);
  const quizzesById = new Map(rollup.quizzes.map((quiz) => [quiz.quizId, quiz]));

  return (
    <main className="mx-auto w-full max-w-3xl px-4 py-8">
      <header className="mb-6">
        <p className="font-body text-sm uppercase tracking-wide text-text-muted">Student standings</p>
        <h1 className="font-display text-2xl text-text-strong">{rollup.classroomName}</h1>
        <p className="mt-1 font-body text-text-muted">
          Each student&rsquo;s overall standing is the average of the quizzes they&rsquo;ve taken.
        </p>
        <Link
          to={`/classrooms/${classroomId}/results`}
          className="mt-2 inline-block font-body text-sm text-text-link"
        >
          Back to class results
        </Link>
      </header>

      {rollup.students.length === 0 ? (
        <Card padding="lg">
          <p className="font-body text-text-body">Nobody has joined this class yet.</p>
        </Card>
      ) : (
        <ul className="flex flex-col gap-3">
          {rollup.students.map((student) => (
            <li key={student.studentId}>
              <StudentStandingCard student={student} quizzesById={quizzesById} />
            </li>
          ))}
        </ul>
      )}

      {pageCount > 1 && (
        <div className="mt-4 flex items-center gap-3">
          <Button size="sm" variant="secondary" disabled={page === 1} onClick={() => setPage((p) => p - 1)}>
            Previous
          </Button>
          <span className="font-body text-sm text-text-muted">
            Page {rollup.page} of {pageCount}
          </span>
          <Button
            size="sm"
            variant="secondary"
            disabled={page >= pageCount}
            onClick={() => setPage((p) => p + 1)}
          >
            Next
          </Button>
        </div>
      )}
    </main>
  );
}

function StudentStandingCard({
  student,
  quizzesById,
}: {
  student: StudentRollupRow;
  quizzesById: Map<string, RollupQuizColumn>;
}) {
  return (
    <Card padding="lg">
      <div className="flex flex-wrap items-baseline justify-between gap-3">
        <h2 className="font-display text-lg text-text-strong">{student.displayName}</h2>
        <p className="font-body text-sm text-text-muted">
          {student.overallStandingPercent === null ? (
            'Not started yet'
          ) : (
            <>
              Overall{' '}
              <span className="font-display text-base text-text-strong">
                {percentLabel(student.overallStandingPercent)}
              </span>
            </>
          )}
        </p>
      </div>

      <dl className="mt-3 flex flex-col gap-1">
        {student.scores.map((score) => {
          const quiz = quizzesById.get(score.quizId);
          return (
            <div key={score.quizId} className="flex flex-wrap items-center justify-between gap-x-3 text-sm">
              <dt className="text-text-muted">{quiz?.title ?? 'Quiz'}</dt>
              <dd className="text-text-body">
                <ScoreCell quizId={score.quizId} studentId={student.studentId} totalPoints={quiz?.totalPoints ?? 0} score={score} />
              </dd>
            </div>
          );
        })}
      </dl>
    </Card>
  );
}

function ScoreCell({
  quizId,
  studentId,
  totalPoints,
  score,
}: {
  quizId: string;
  studentId: string;
  totalPoints: number;
  score: StudentQuizScore;
}) {
  if (score.status === 'Completed' && score.score !== null && score.attemptId !== null) {
    return (
      <Link
        to={`/quizzes/${quizId}/results/students/${studentId}`}
        className="font-body text-text-link"
      >
        {scoreLabel(score.score, totalPoints)}
        {score.percent !== null && <> · {percentLabel(score.percent)}</>}
      </Link>
    );
  }
  return (
    <span className="text-text-muted">{score.status === 'InProgress' ? 'In progress' : 'Not taken'}</span>
  );
}
