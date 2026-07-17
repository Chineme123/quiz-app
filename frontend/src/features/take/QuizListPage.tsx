import { useState } from 'react';
import { useNavigate } from 'react-router';
import { Button, Card } from '@/components/ui';
import { useAvailableQuizzes } from './useTakeQueries';
import { startQuiz } from './take.api';
import type { AvailableQuiz } from './take.schemas';

/**
 * The quizzes a student can take (spec 0006, AC-1, AC-2). This is the entry point that makes
 * the loop drivable by a person instead of by curl: every row offers the one action that makes
 * sense for it, and a quiz already running carries its attempt id so Resume can never start a
 * second attempt by accident.
 */
export function QuizListPage() {
  const { data, isPending, isError, refetch } = useAvailableQuizzes();
  const navigate = useNavigate();
  const [startingId, setStartingId] = useState<string | null>(null);
  const [startError, setStartError] = useState<string | null>(null);

  async function handleStart(quiz: AvailableQuiz) {
    setStartingId(quiz.quizId);
    setStartError(null);
    try {
      const attemptId = await startQuiz(quiz.quizId);
      void navigate(`/attempts/${attemptId}/take`);
    } catch {
      // The server refuses for reasons the student should hear plainly: not enrolled, out of
      // attempts, or the window has closed.
      setStartError("That quiz can't be started right now. Refresh and try again.");
      setStartingId(null);
    }
  }

  return (
    <main className="mx-auto w-full max-w-3xl px-4 py-8">
      <header className="mb-6">
        <h1 className="font-display text-2xl text-text-strong">Your quizzes</h1>
        <p className="mt-1 font-body text-text-muted">Everything your classrooms have set for you.</p>
      </header>

      {isPending && <p className="font-body text-text-muted">Loading your quizzes…</p>}

      {isError && (
        <Card padding="lg">
          <p className="font-body text-text-body">We couldn't load your quizzes just now.</p>
          <Button className="mt-4" onClick={() => void refetch()}>
            Try again
          </Button>
        </Card>
      )}

      {startError && (
        <p role="alert" className="mb-4 font-body text-danger">
          {startError}
        </p>
      )}

      {data && data.items.length === 0 && (
        <Card padding="lg">
          <p className="font-body text-text-body">
            No quizzes yet. When a teacher sets one for a classroom you're in, it'll show up here.
          </p>
        </Card>
      )}

      {data && data.items.length > 0 && (
        <ul className="flex flex-col gap-3">
          {data.items.map((quiz) => (
            <li key={quiz.quizId}>
              <Card padding="lg">
                <div className="flex flex-wrap items-center justify-between gap-4">
                  <div className="min-w-0">
                    <h2 className="font-display text-lg text-text-strong">{quiz.title}</h2>
                    <p className="mt-1 font-body text-sm text-text-muted">
                      {quiz.questionCount} {quiz.questionCount === 1 ? 'question' : 'questions'} ·{' '}
                      {quiz.durationMinutes} min
                    </p>
                  </div>

                  {quiz.state === 'NotStarted' && (
                    <Button
                      onClick={() => void handleStart(quiz)}
                      loading={startingId === quiz.quizId}
                      disabled={startingId !== null}
                    >
                      Start
                    </Button>
                  )}

                  {quiz.state === 'InProgress' && quiz.attemptId && (
                    <Button variant="primary" onClick={() => void navigate(`/attempts/${quiz.attemptId}/take`)}>
                      Resume
                    </Button>
                  )}

                  {quiz.state === 'Graded' && quiz.attemptId && (
                    <Button variant="secondary" onClick={() => void navigate(`/results/${quiz.attemptId}`)}>
                      View result
                    </Button>
                  )}
                </div>
              </Card>
            </li>
          ))}
        </ul>
      )}
    </main>
  );
}
