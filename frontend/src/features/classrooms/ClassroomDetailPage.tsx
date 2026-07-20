import { useState } from 'react';
import { useNavigate, useParams } from 'react-router';
import { Archive, ArrowCounterClockwise, UserMinus } from '@phosphor-icons/react';
import { Button, Card, Dialog, Icon, TextField, useToast } from '@/components/ui';
import { toUserMessage } from '@/lib/api/errorMessage';
import { JoinCode } from './JoinCode';
import {
  useArchiveClassroom,
  useClassroom,
  useClassroomRoster,
  useRegenerateJoinCode,
  useRemoveStudent,
  useRenameClassroom,
  useUnarchiveClassroom,
} from './useClassroomQueries';

/**
 * One class, as its teacher manages it (spec 0008, AC-7, AC-8, AC-10): rename it, re issue its
 * code, archive or restore it, and see and prune the roster.
 *
 * Archiving and removing a student both go through a Dialog first. Neither destroys anything,
 * but both change what a student can reach, and `ui-rules.md` §1 makes a gentle confirm with
 * plain stakes non negotiable for that.
 */
export function ClassroomDetailPage() {
  const { classroomId = '' } = useParams<{ classroomId: string }>();
  const navigate = useNavigate();
  const toast = useToast();

  const { data: classroom, isPending, isError, refetch } = useClassroom(classroomId);
  const [page, setPage] = useState(1);
  const roster = useClassroomRoster(classroomId, page);

  const rename = useRenameClassroom(classroomId);
  const archive = useArchiveClassroom(classroomId);
  const unarchive = useUnarchiveClassroom(classroomId);
  const regenerate = useRegenerateJoinCode(classroomId);
  const removeStudent = useRemoveStudent(classroomId);

  const [nameError, setNameError] = useState<string | null>(null);
  const [confirming, setConfirming] = useState<'archive' | { studentId: string } | null>(null);

  function handleRename(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    // Read the field itself rather than mirroring it in state: the input is seeded from the
    // loaded class, so a separate state would start empty and reject a submit the user never
    // edited.
    const entry = new FormData(event.currentTarget).get('className');
    const trimmed = (typeof entry === 'string' ? entry : '').trim();
    if (trimmed === '') {
      setNameError('Give your class a name so students recognise it.');
      return;
    }
    setNameError(null);
    rename.mutate(trimmed, {
      onSuccess: () => toast.show({ tone: 'success', message: 'Class renamed.' }),
      onError: (error) =>
        toast.show({ tone: 'danger', message: toUserMessage(error, "We couldn't rename it just now.") }),
    });
  }

  if (isPending) {
    return <p className="mx-auto w-full max-w-3xl px-4 py-8 font-body text-text-muted">Loading this class…</p>;
  }

  if (isError) {
    return (
      <main className="mx-auto w-full max-w-3xl px-4 py-8">
        <Card padding="lg">
          <p className="font-body text-text-body">We couldn't load this class just now.</p>
          <Button className="mt-4" onClick={() => void refetch()}>
            Try again
          </Button>
        </Card>
      </main>
    );
  }

  // Null covers "no such class" and "not yours" alike, so nothing here reveals which (AC-11).
  if (!classroom) {
    return (
      <main className="mx-auto w-full max-w-3xl px-4 py-8">
        <Card padding="lg">
          <p className="font-body text-text-body">We couldn't find that class.</p>
          <Button className="mt-4" variant="secondary" onClick={() => void navigate('/dashboard')}>
            Back to your classes
          </Button>
        </Card>
      </main>
    );
  }

  const isArchived = Boolean(classroom.archivedAt);

  return (
    <main className="mx-auto w-full max-w-3xl px-4 py-8">
      <header className="mb-6">
        <h1 className="font-display text-2xl text-text-strong">{classroom.name}</h1>
        <p className="mt-1 font-body text-text-muted">
          {classroom.studentCount ?? 0} {classroom.studentCount === 1 ? 'student' : 'students'} ·{' '}
          {classroom.quizCount ?? 0} {classroom.quizCount === 1 ? 'quiz' : 'quizzes'}
          {isArchived && ' · archived'}
        </p>
      </header>

      {isArchived && (
        <Card padding="lg" className="mb-6">
          <p className="font-body text-text-body">
            This class is put away. Students can't join it or take its quizzes, and everything is
            kept exactly as it was.
          </p>
          <Button
            className="mt-4"
            icon={ArrowCounterClockwise}
            loading={unarchive.isPending}
            onClick={() =>
              unarchive.mutate(undefined, {
                onSuccess: () => toast.show({ tone: 'success', message: 'Class restored.' }),
              })
            }
          >
            Restore this class
          </Button>
        </Card>
      )}

      {!isArchived && classroom.joinCode && (
        <Card padding="lg" className="mb-6">
          <div className="flex flex-wrap items-start justify-between gap-4">
            <JoinCode code={classroom.joinCode} />
            <Button
              variant="secondary"
              loading={regenerate.isPending}
              onClick={() =>
                regenerate.mutate(undefined, {
                  onSuccess: () =>
                    toast.show({
                      tone: 'success',
                      title: 'New code issued',
                      message: 'The old code and its link no longer work.',
                    }),
                })
              }
            >
              Issue a new code
            </Button>
          </div>
          <p className="mt-3 font-body text-sm text-text-muted">
            Issuing a new code stops the old one working, which is how you shut off a code that
            got out. Students already in the class stay in it.
          </p>
        </Card>
      )}

      <Card padding="lg" className="mb-6">
        <h2 className="mb-3 font-display text-lg text-text-strong">Class name</h2>
        <form onSubmit={handleRename} className="flex flex-wrap items-end gap-3">
          <div className="min-w-56 flex-1">
            <TextField
              label="Rename this class"
              name="className"
              defaultValue={classroom.name}
              error={nameError ?? undefined}
              maxLength={100}
            />
          </div>
          <Button type="submit" variant="secondary" loading={rename.isPending}>
            Save name
          </Button>
        </form>
      </Card>

      <Card padding="lg" className="mb-6">
        <h2 className="mb-3 font-display text-lg text-text-strong">Students</h2>

        {roster.isPending && <p className="font-body text-text-muted">Loading the roster…</p>}

        {roster.data && roster.data.total === 0 && (
          <p className="font-body text-text-body">
            Nobody has joined yet. Share the code above and they'll appear here.
          </p>
        )}

        {roster.data && roster.data.total > 0 && (
          <>
            <ul className="flex flex-col gap-2">
              {roster.data.items.map((entry) => (
                <li
                  key={entry.studentId}
                  className="flex flex-wrap items-center justify-between gap-3 border-b border-border py-2 last:border-b-0"
                >
                  {/* No display name yet: student names live in the Identity module behind a
                      plain Guid (spec 0007), so this shows what the Assessment module knows. */}
                  <span className="font-mono text-sm text-text-body">{entry.studentId}</span>
                  <Button
                    size="sm"
                    variant="ghost"
                    icon={UserMinus}
                    onClick={() => setConfirming({ studentId: entry.studentId })}
                  >
                    Remove
                  </Button>
                </li>
              ))}
            </ul>

            {roster.data.total > roster.data.pageSize && (
              <div className="mt-4 flex items-center gap-3">
                <Button size="sm" variant="secondary" disabled={page === 1} onClick={() => setPage((p) => p - 1)}>
                  Previous
                </Button>
                <span className="font-body text-sm text-text-muted">
                  Page {roster.data.page} of {Math.ceil(roster.data.total / roster.data.pageSize)}
                </span>
                <Button
                  size="sm"
                  variant="secondary"
                  disabled={page >= Math.ceil(roster.data.total / roster.data.pageSize)}
                  onClick={() => setPage((p) => p + 1)}
                >
                  Next
                </Button>
              </div>
            )}
          </>
        )}
      </Card>

      {!isArchived && (
        <Card padding="lg">
          <h2 className="mb-2 font-display text-lg text-text-strong">Put this class away</h2>
          <p className="mb-4 font-body text-text-body">
            Archiving hides the class from your students and stops its quizzes. Nothing is
            deleted, and you can restore it whenever you like.
          </p>
          <Button
            variant="secondary"
            icon={Archive}
            onClick={() => setConfirming('archive')}
          >
            Archive this class
          </Button>
        </Card>
      )}

      <Dialog
        open={confirming === 'archive'}
        onClose={() => setConfirming(null)}
        title="Archive this class?"
        description="Your students will no longer see it or be able to take its quizzes. Every quiz, result, and student stays exactly as it is, and you can restore it at any time."
        icon={<Icon icon={Archive} weight="fill" />}
        tone="warning"
        footer={
          <>
            <Button variant="secondary" onClick={() => setConfirming(null)}>
              Keep it open
            </Button>
            <Button
              loading={archive.isPending}
              onClick={() =>
                archive.mutate(undefined, {
                  onSuccess: () => {
                    setConfirming(null);
                    toast.show({ tone: 'success', message: 'Class archived. You can restore it any time.' });
                  },
                })
              }
            >
              Yes, archive it
            </Button>
          </>
        }
      />

      <Dialog
        open={typeof confirming === 'object' && confirming !== null}
        onClose={() => setConfirming(null)}
        title="Remove this student?"
        description="They'll lose access to this class and its quizzes. Anything they've already submitted is kept, and they can join again with the code."
        icon={<Icon icon={UserMinus} weight="fill" />}
        tone="danger"
        footer={
          <>
            <Button variant="secondary" onClick={() => setConfirming(null)}>
              Cancel
            </Button>
            <Button
              loading={removeStudent.isPending}
              onClick={() => {
                if (typeof confirming !== 'object' || confirming === null) return;
                removeStudent.mutate(confirming.studentId, {
                  onSuccess: () => {
                    setConfirming(null);
                    toast.show({ tone: 'success', message: 'Student removed from this class.' });
                  },
                });
              }}
            >
              Yes, remove them
            </Button>
          </>
        }
      />
    </main>
  );
}
