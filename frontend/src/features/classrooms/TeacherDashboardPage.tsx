import { useState } from 'react';
import { Link } from 'react-router';
import { Button, Card, TextField, useToast } from '@/components/ui';
import { toUserMessage } from '@/lib/api/errorMessage';
import { useCreateClassroom, useOwnedClassrooms } from './useClassroomQueries';
import { JoinCode } from './JoinCode';
import type { OwnedClassroom } from './classrooms.schemas';

/**
 * The teacher's home (spec 0008, AC-2): every class they run, with the code students join by
 * and a link to hand out. Creating a class is the first thing a new teacher needs, so the form
 * sits at the top rather than behind a menu.
 */
export function TeacherDashboardPage() {
  const { data, isPending, isError, refetch } = useOwnedClassrooms();
  const createClassroom = useCreateClassroom();
  const toast = useToast();
  const [name, setName] = useState('');
  const [nameError, setNameError] = useState<string | null>(null);

  function handleCreate(event: React.FormEvent) {
    event.preventDefault();
    const trimmed = name.trim();

    if (trimmed === '') {
      setNameError('Give your class a name so students recognise it.');
      return;
    }

    setNameError(null);
    createClassroom.mutate(trimmed, {
      onSuccess: (classroom) => {
        setName('');
        toast.show({
          tone: 'success',
          title: 'Class created',
          message: `Students can join ${classroom.name} with the code ${classroom.joinCode}.`,
        });
      },
      onError: (error) => {
        toast.show({
          tone: 'danger',
          message: toUserMessage(error, "We couldn't create that class just now."),
        });
      },
    });
  }

  const classes = data ?? [];
  const active = classes.filter((c) => !c.archivedAt);
  const archived = classes.filter((c) => c.archivedAt);

  return (
    <main className="mx-auto w-full max-w-3xl px-4 py-8">
      <header className="mb-6">
        <h1 className="font-display text-2xl text-text-strong">Your classes</h1>
        <p className="mt-1 font-body text-text-muted">
          Create a class, then share its code so your students can join.
        </p>
      </header>

      <Card padding="lg" className="mb-6">
        <form onSubmit={handleCreate} className="flex flex-wrap items-end gap-3">
          <div className="min-w-56 flex-1">
            <TextField
              label="Class name"
              placeholder="Biology 101"
              value={name}
              onChange={(event) => setName(event.target.value)}
              error={nameError ?? undefined}
              maxLength={100}
            />
          </div>
          <Button type="submit" loading={createClassroom.isPending}>
            Create class
          </Button>
        </form>
      </Card>

      {isPending && <p className="font-body text-text-muted">Loading your classes…</p>}

      {isError && (
        <Card padding="lg">
          <p className="font-body text-text-body">We couldn't load your classes just now.</p>
          <Button className="mt-4" onClick={() => void refetch()}>
            Try again
          </Button>
        </Card>
      )}

      {data && classes.length === 0 && (
        <Card padding="lg">
          <p className="font-body text-text-body">
            No classes yet. Create your first one above, and you'll get a code to share.
          </p>
        </Card>
      )}

      {active.length > 0 && (
        <ul className="flex flex-col gap-3">
          {active.map((classroom) => (
            <li key={classroom.id}>
              <ClassroomRow classroom={classroom} />
            </li>
          ))}
        </ul>
      )}

      {archived.length > 0 && (
        <section className="mt-8">
          <h2 className="mb-3 font-display text-lg text-text-strong">Archived</h2>
          <p className="mb-3 font-body text-sm text-text-muted">
            These are put away. Students can't join or take their quizzes, and everything is kept.
          </p>
          <ul className="flex flex-col gap-3">
            {archived.map((classroom) => (
              <li key={classroom.id}>
                <ClassroomRow classroom={classroom} />
              </li>
            ))}
          </ul>
        </section>
      )}
    </main>
  );
}

function ClassroomRow({ classroom }: { classroom: OwnedClassroom }) {
  const students = `${classroom.studentCount} ${classroom.studentCount === 1 ? 'student' : 'students'}`;
  const quizzes = `${classroom.quizCount} ${classroom.quizCount === 1 ? 'quiz' : 'quizzes'}`;

  return (
    <Card padding="lg">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div className="min-w-0">
          <h2 className="font-display text-lg text-text-strong">
            <Link to={`/classrooms/${classroom.id}`} className="hover:text-primary">
              {classroom.name}
            </Link>
          </h2>
          <p className="mt-1 font-body text-sm text-text-muted">
            {students} · {quizzes}
          </p>
        </div>

        {!classroom.archivedAt && <JoinCode code={classroom.joinCode} />}
      </div>
    </Card>
  );
}
