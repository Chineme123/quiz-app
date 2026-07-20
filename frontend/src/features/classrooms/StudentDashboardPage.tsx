import { useState } from 'react';
import { Link, useNavigate } from 'react-router';
import { SignOut } from '@phosphor-icons/react';
import { Button, Card, Dialog, Icon, TextField, useToast } from '@/components/ui';
import { ApiError } from '@/lib/api/errors';
import { toUserMessage } from '@/lib/api/errorMessage';
import { useEnrolledClassrooms, useJoinClassroom, useLeaveClassroom } from './useClassroomQueries';
import type { EnrolledClassroom } from './classrooms.schemas';

/**
 * The student's home (spec 0008, AC-5): the classes they are in, and the one box that gets them
 * into a new one. Joining is the thing a student does first, so the code box leads.
 */
export function StudentDashboardPage() {
  const { data, isPending, isError, refetch } = useEnrolledClassrooms();
  const joinClassroom = useJoinClassroom();
  const navigate = useNavigate();
  const toast = useToast();
  const leaveClassroom = useLeaveClassroom();
  const [code, setCode] = useState('');
  const [codeError, setCodeError] = useState<string | null>(null);
  const [leaving, setLeaving] = useState<EnrolledClassroom | null>(null);

  function handleJoin(event: React.FormEvent) {
    event.preventDefault();
    const trimmed = code.trim();

    if (trimmed === '') {
      setCodeError('Enter the code your teacher gave you.');
      return;
    }

    setCodeError(null);
    joinClassroom.mutate(trimmed, {
      onSuccess: (joined) => {
        setCode('');
        toast.show({
          tone: 'success',
          title: "You're in",
          message: `You joined ${joined.name}.`,
        });
      },
      onError: (error) => {
        // The server tells these two apart, and they need different words: a code that opens
        // nothing, versus a class you already teach.
        if (error instanceof ApiError && error.status === 404) {
          setCodeError("That code doesn't open any class. Check it and try again.");
          return;
        }
        if (error instanceof ApiError && error.status === 409) {
          setCodeError('You teach this class, so you cannot join it as a student.');
          return;
        }
        toast.show({
          tone: 'danger',
          message: toUserMessage(error, "We couldn't join that class just now."),
        });
      },
    });
  }

  const classes = data ?? [];

  return (
    <main className="mx-auto w-full max-w-3xl px-4 py-8">
      <header className="mb-6">
        <h1 className="font-display text-2xl text-text-strong">Your classes</h1>
        <p className="mt-1 font-body text-text-muted">
          Join a class with the code your teacher shared, then take its quizzes.
        </p>
      </header>

      <Card padding="lg" className="mb-6">
        <form onSubmit={handleJoin} className="flex flex-wrap items-end gap-3">
          <div className="min-w-48 flex-1">
            <TextField
              label="Class code"
              placeholder="ABC234"
              value={code}
              onChange={(event) => setCode(event.target.value.toUpperCase())}
              error={codeError ?? undefined}
              autoCapitalize="characters"
              autoComplete="off"
              spellCheck={false}
              maxLength={6}
              className="font-mono tracking-widest"
            />
          </div>
          <Button type="submit" loading={joinClassroom.isPending}>
            Join class
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
            You're not in any classes yet. Enter the code your teacher gave you above.
          </p>
        </Card>
      )}

      {classes.length > 0 && (
        <ul className="flex flex-col gap-3">
          {classes.map((classroom) => (
            <li key={classroom.id}>
              <Card padding="lg">
                <div className="flex flex-wrap items-center justify-between gap-4">
                  <h2 className="min-w-0 font-display text-lg text-text-strong">{classroom.name}</h2>
                  <div className="flex flex-wrap gap-2">
                    <Button variant="secondary" onClick={() => void navigate('/quizzes')}>
                      See quizzes
                    </Button>
                    <Button
                      variant="ghost"
                      icon={SignOut}
                      onClick={() => setLeaving(classroom)}
                    >
                      Leave
                    </Button>
                  </div>
                </div>
              </Card>
            </li>
          ))}
        </ul>
      )}

      {classes.length > 0 && (
        <p className="mt-6 font-body text-sm text-text-muted">
          Looking for something to do? <Link to="/quizzes" className="text-primary">All your quizzes</Link> are in one place.
        </p>
      )}

      {/* Leaving is reversible (the code lets you back in) but it does change what you can
          reach, so it gets a plain spoken confirm rather than happening on one tap. */}
      <Dialog
        open={leaving !== null}
        onClose={() => setLeaving(null)}
        title={leaving ? `Leave ${leaving.name}?` : 'Leave this class?'}
        description="You'll stop seeing its quizzes. Anything you've already submitted is kept, and you can join again with the class code."
        icon={<Icon icon={SignOut} weight="fill" />}
        tone="warning"
        footer={
          <>
            <Button variant="secondary" onClick={() => setLeaving(null)}>
              Stay in the class
            </Button>
            <Button
              loading={leaveClassroom.isPending}
              onClick={() => {
                if (!leaving) return;
                leaveClassroom.mutate(leaving.id, {
                  onSuccess: () => {
                    toast.show({ tone: 'success', message: `You left ${leaving.name}.` });
                    setLeaving(null);
                  },
                  onError: (error) =>
                    toast.show({
                      tone: 'danger',
                      message: toUserMessage(error, "We couldn't leave that class just now."),
                    }),
                });
              }}
            >
              Yes, leave
            </Button>
          </>
        }
      />
    </main>
  );
}
