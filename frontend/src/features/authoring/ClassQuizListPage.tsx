import { useState, type FormEvent } from 'react';
import { Link, useNavigate, useParams } from 'react-router';
import { Button, Card, TextField, useToast } from '@/components/ui';
import { toUserMessage } from '@/lib/api/errorMessage';
import { useClassroomQuizzes, useCreateQuiz } from './useAuthoringQueries';

/**
 * The teacher's quizzes in one class (spec 0009, AC-10): create a quiz, see each one's state at a
 * glance, and open it in the editor. Owner scoped, so a class that is not yours reads as missing.
 */
export function ClassQuizListPage() {
  const { classroomId = '' } = useParams<{ classroomId: string }>();
  const navigate = useNavigate();
  const toast = useToast();

  const quizzes = useClassroomQuizzes(classroomId);
  const createQuiz = useCreateQuiz(classroomId);

  const [title, setTitle] = useState('');
  const [minutes, setMinutes] = useState('10');
  const [formError, setFormError] = useState<string | null>(null);

  function handleCreate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFormError(null);

    const duration = Number(minutes);
    if (title.trim() === '') {
      setFormError('Give your quiz a title.');
      return;
    }
    if (!Number.isFinite(duration) || duration < 1) {
      setFormError('Set how many minutes students get, at least one.');
      return;
    }

    createQuiz.mutate(
      { title: title.trim(), durationMinutes: duration },
      {
        onSuccess: (quiz) => {
          setTitle('');
          toast.show({ tone: 'success', message: 'Quiz created. Add some questions next.' });
          void navigate(`/quizzes/${quiz.id}/edit`);
        },
        onError: (error) => {
          toast.show({
            tone: 'danger',
            message: toUserMessage(error, "We couldn't create that quiz just now."),
          });
        },
      },
    );
  }

  if (quizzes.isPending) {
    return (
      <main className="mx-auto w-full max-w-3xl px-4 py-8">
        <p className="font-body text-text-muted">Loading your quizzes…</p>
      </main>
    );
  }

  if (quizzes.isError) {
    return (
      <main className="mx-auto w-full max-w-3xl px-4 py-8">
        <Card padding="lg">
          <p className="font-body text-text-body">We couldn&rsquo;t load this class&rsquo;s quizzes.</p>
          <Button className="mt-4" onClick={() => void quizzes.refetch()}>
            Try again
          </Button>
        </Card>
      </main>
    );
  }

  // Null means the class is not yours, or there is no such class: the same answer either way.
  if (quizzes.data === null) {
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

  return (
    <main className="mx-auto w-full max-w-3xl px-4 py-8">
      <h1 className="font-display text-2xl text-text-strong">Quizzes</h1>
      <p className="mt-1 font-body text-text-muted">
        Build a quiz for this class, then publish it when it&rsquo;s ready for students.
      </p>
      <Link to={`/classrooms/${classroomId}`} className="mt-2 inline-block font-body text-sm text-text-link">
        Back to the class
      </Link>

      <Card padding="lg" className="mb-6 mt-6">
        <h2 className="mb-3 font-display text-lg text-text-strong">New quiz</h2>
        <form onSubmit={handleCreate} noValidate className="flex flex-col gap-4">
          <TextField
            label="Title"
            required
            value={title}
            onChange={(event) => setTitle(event.target.value)}
            error={formError ?? undefined}
          />
          <TextField
            label="Minutes to finish"
            type="number"
            min={1}
            required
            value={minutes}
            onChange={(event) => setMinutes(event.target.value)}
            hint="How long a student gets once they start."
          />
          <div>
            <Button type="submit" loading={createQuiz.isPending}>
              Create quiz
            </Button>
          </div>
        </form>
      </Card>

      {quizzes.data.length === 0 ? (
        <Card padding="lg">
          <p className="font-body text-text-body">
            No quizzes here yet. Create your first one above, then add some questions.
          </p>
        </Card>
      ) : (
        <ul className="flex flex-col gap-3">
          {quizzes.data.map((quiz) => (
            <li key={quiz.id}>
              <Card padding="lg">
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <div>
                    <h3 className="font-display text-lg text-text-strong">{quiz.title}</h3>
                    <p className="font-body text-sm text-text-muted">
                      {quiz.isPublished ? 'Published' : 'Draft'} ·{' '}
                      {quiz.questionCount === 1 ? '1 question' : `${quiz.questionCount} questions`}
                      {quiz.attemptCount > 0
                        ? ` · ${quiz.attemptCount === 1 ? '1 attempt so far' : `${quiz.attemptCount} attempts so far`}`
                        : ''}
                    </p>
                  </div>
                  <Button
                    variant="secondary"
                    onClick={() => void navigate(`/quizzes/${quiz.id}/edit`)}
                  >
                    Edit
                  </Button>
                </div>
              </Card>
            </li>
          ))}
        </ul>
      )}
    </main>
  );
}
