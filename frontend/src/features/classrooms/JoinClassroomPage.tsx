import { useNavigate, useParams } from 'react-router';
import { Button, Card, useToast } from '@/components/ui';
import { ApiError } from '@/lib/api/errors';
import { toUserMessage } from '@/lib/api/errorMessage';
import { useClassroomByCode, useJoinClassroom } from './useClassroomQueries';

/**
 * What a join link opens (spec 0008, AC-4). The screen names the class first and waits for a
 * deliberate confirm: opening a link should never quietly enrol you somewhere.
 *
 * Arriving signed out is handled upstream by RequireAuth, which sends you to `/sign-in` and
 * back here afterwards, so one link works for a brand new student in a single pass.
 */
export function JoinClassroomPage() {
  const { code = '' } = useParams<{ code: string }>();
  const normalized = code.trim().toUpperCase();
  const { data: preview, isPending, isError, refetch } = useClassroomByCode(normalized);
  const joinClassroom = useJoinClassroom();
  const navigate = useNavigate();
  const toast = useToast();

  function handleJoin() {
    joinClassroom.mutate(normalized, {
      onSuccess: (joined) => {
        toast.show({
          tone: 'success',
          title: "You're in",
          message: `You joined ${joined.name}.`,
        });
        void navigate('/dashboard');
      },
      onError: (error) => {
        if (error instanceof ApiError && error.status === 409) {
          toast.show({
            tone: 'warning',
            message: 'You teach this class, so you cannot join it as a student.',
          });
          return;
        }
        toast.show({
          tone: 'danger',
          message: toUserMessage(error, "We couldn't join that class just now."),
        });
      },
    });
  }

  return (
    <main className="mx-auto w-full max-w-xl px-4 py-8">
      <header className="mb-6">
        <h1 className="font-display text-2xl text-text-strong">Join a class</h1>
      </header>

      {isPending && <p className="font-body text-text-muted">Looking up that code…</p>}

      {isError && (
        <Card padding="lg">
          <p className="font-body text-text-body">We couldn't check that code just now.</p>
          <Button className="mt-4" onClick={() => void refetch()}>
            Try again
          </Button>
        </Card>
      )}

      {/* A code that opens nothing and an archived class read the same, deliberately: a link
          never reveals that a class exists. */}
      {!isPending && !isError && preview === null && (
        <Card padding="lg">
          <p className="font-body text-text-body">
            That link doesn't open any class. Ask your teacher for a fresh one.
          </p>
          <Button className="mt-4" variant="secondary" onClick={() => void navigate('/dashboard')}>
            Back to your classes
          </Button>
        </Card>
      )}

      {preview && (
        <Card padding="lg">
          <p className="font-body text-text-muted">You've been invited to join</p>
          <h2 className="mt-1 font-display text-xl text-text-strong">{preview.name}</h2>

          {preview.isOwner && (
            <p className="mt-4 font-body text-text-body">
              This is your own class, so there's nothing to join. Open it to see who's in it.
            </p>
          )}

          {!preview.isOwner && preview.alreadyEnrolled && (
            <p className="mt-4 font-body text-text-body">You're already in this class.</p>
          )}

          <div className="mt-6 flex flex-wrap gap-3">
            {!preview.isOwner && !preview.alreadyEnrolled && (
              <Button onClick={handleJoin} loading={joinClassroom.isPending}>
                Join class
              </Button>
            )}

            <Button
              variant={preview.isOwner || preview.alreadyEnrolled ? 'primary' : 'secondary'}
              onClick={() => void navigate('/dashboard')}
            >
              {preview.isOwner || preview.alreadyEnrolled ? 'Go to your classes' : 'Not now'}
            </Button>
          </div>
        </Card>
      )}
    </main>
  );
}
