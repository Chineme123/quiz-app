import { useState } from 'react';
import { Link, useParams } from 'react-router';
import { Button, Card } from '@/components/ui';
import { useQuizResults } from './useClassroomResultsQueries';
import { percentLabel, scoreLabel } from './resultsFormat';
import type { QuestionDifficulty, QuizStudentResult } from './classroomResults.schemas';

/**
 * One quiz's results for its owning teacher (spec 0010, AC-3, AC-4): the class average, which
 * questions tripped the class up, and a paginated list of students with their latest submitted
 * score or a Not taken / In progress marker. Each finished student opens their attempt (AC-6).
 * Names come resolved from Identity (AC-13); a low score is framed to review, never as a failure.
 */
export function QuizResultsPage() {
  const { quizId = '' } = useParams<{ quizId: string }>();
  const [page, setPage] = useState(1);
  const query = useQuizResults(quizId, page);

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

  if (query.data === null) {
    return (
      <main className="mx-auto w-full max-w-3xl px-4 py-8">
        <Card padding="lg">
          <h1 className="font-display text-2xl text-text-strong">We couldn&rsquo;t find that quiz</h1>
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
  const pageCount = Math.ceil(results.total / results.pageSize);

  return (
    <main className="mx-auto w-full max-w-3xl px-4 py-8">
      <header className="mb-6">
        <p className="font-body text-sm uppercase tracking-wide text-text-muted">Quiz results</p>
        <h1 className="font-display text-2xl text-text-strong">{results.title}</h1>
        <p className="mt-1 font-body text-text-muted">
          {results.completionCount} of {results.studentCount} finished
          {results.completionCount > 0 && results.averagePercent !== null && (
            <> · class average {percentLabel(results.averagePercent)}</>
          )}
        </p>
        <Link
          to={`/classrooms/${results.classroomId}/results`}
          className="mt-2 inline-block font-body text-sm text-text-link"
        >
          Back to class results
        </Link>
      </header>

      <section aria-labelledby="difficulty-heading" className="mb-6">
        <h2 id="difficulty-heading" className="mb-3 font-display text-lg text-text-strong">
          Where the class stands, question by question
        </h2>
        {results.questions.length === 0 ? (
          <Card padding="lg">
            <p className="font-body text-text-body">This quiz has no questions yet.</p>
          </Card>
        ) : (
          <ul className="flex flex-col gap-3">
            {results.questions.map((question, index) => (
              <li key={question.questionId}>
                <QuestionDifficultyCard index={index + 1} question={question} />
              </li>
            ))}
          </ul>
        )}
      </section>

      <section aria-labelledby="students-heading">
        <h2 id="students-heading" className="mb-3 font-display text-lg text-text-strong">
          Students
        </h2>
        <ul className="flex flex-col gap-2">
          {results.students.map((student) => (
            <li key={student.studentId}>
              <StudentRow quizId={results.quizId} quizTitle={results.title} totalPoints={results.totalPoints} student={student} />
            </li>
          ))}
        </ul>

        {pageCount > 1 && (
          <div className="mt-4 flex items-center gap-3">
            <Button size="sm" variant="secondary" disabled={page === 1} onClick={() => setPage((p) => p - 1)}>
              Previous
            </Button>
            <span className="font-body text-sm text-text-muted">
              Page {results.page} of {pageCount}
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
      </section>
    </main>
  );
}

function QuestionDifficultyCard({ index, question }: { index: number; question: QuestionDifficulty }) {
  const worthReviewing = question.fractionCorrect !== null && question.fractionCorrect < 50;
  return (
    <Card padding="lg">
      <div className="flex items-center justify-between gap-3">
        <span className="font-body text-sm text-text-muted">Question {index}</span>
        {worthReviewing && (
          <span className="rounded-full bg-ai-surface px-3 py-1 text-xs font-bold text-ai-accent">
            Worth reviewing together
          </span>
        )}
      </div>
      <p className="mt-2 font-display text-lg text-text-strong">{question.prompt}</p>
      <p className="mt-2 font-body text-sm text-text-muted">
        {question.answeredCount === 0 ? (
          'No one has answered this yet.'
        ) : (
          <>
            {question.correctCount} of {question.answeredCount} got this right
            {question.fractionCorrect !== null && <> · {percentLabel(question.fractionCorrect)}</>}
          </>
        )}
      </p>
    </Card>
  );
}

function StudentRow({
  quizId,
  quizTitle,
  totalPoints,
  student,
}: {
  quizId: string;
  quizTitle: string;
  totalPoints: number;
  student: QuizStudentResult;
}) {
  return (
    <div className="flex flex-wrap items-center justify-between gap-3 border-b border-border py-2 last:border-b-0">
      <span className="font-body text-text-body">{student.displayName}</span>
      {student.status === 'Completed' && student.score !== null && student.attemptId !== null ? (
        <Link
          to={`/quizzes/${quizId}/results/students/${student.studentId}`}
          state={{ displayName: student.displayName, quizTitle }}
          className="font-body text-sm text-text-link"
        >
          {scoreLabel(student.score, totalPoints)}
          {student.percent !== null && <> · {percentLabel(student.percent)}</>}
        </Link>
      ) : (
        <span className="font-body text-sm text-text-muted">
          {student.status === 'InProgress' ? 'In progress' : 'Not taken'}
        </span>
      )}
    </div>
  );
}
