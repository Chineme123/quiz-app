import { useState, type FormEvent } from 'react';
import { Link, useParams } from 'react-router';
import { Button, Card, Dialog, TextField, useToast } from '@/components/ui';
import { toUserMessage } from '@/lib/api/errorMessage';
import { GenerationSection } from './GenerationSection';
import { QuestionForm } from './QuestionForm';
import { TYPE_LABEL, answerSummary, pointsLabel } from './questionDisplay';
import type { QuestionInput } from './authoring.api';
import type { AuthoredQuestion } from './authoring.schemas';
import {
  useAddQuestion,
  useDeleteQuestion,
  useEditQuestion,
  usePublishQuiz,
  useQuiz,
  useUnpublishQuiz,
} from './useAuthoringQueries';

/** An ISO instant trimmed to what a datetime-local input understands. */
function toInputValue(iso: string | null): string {
  return iso === null ? '' : iso.slice(0, 16);
}

type DialogState =
  | { kind: 'add' }
  | { kind: 'edit'; question: AuthoredQuestion }
  | { kind: 'remove'; question: AuthoredQuestion }
  | { kind: 'publish' }
  | { kind: 'unpublish' }
  | null;

/**
 * The quiz editor (spec 0009, AC-3, AC-9, AC-10): build the question set by hand, set when the quiz
 * is available and how many attempts it allows, and publish it so the class can take it. Once a
 * student has an attempt the question set is fixed, and the editor says so rather than letting a
 * teacher try and be refused.
 */
export function QuizEditorPage() {
  const { quizId = '' } = useParams<{ quizId: string }>();
  const toast = useToast();

  const quizQuery = useQuiz(quizId);
  const quiz = quizQuery.data ?? null;
  const classroomId = quiz?.classroomId;

  const addQuestion = useAddQuestion(quizId, classroomId);
  const editQuestion = useEditQuestion(quizId, classroomId);
  const removeQuestion = useDeleteQuestion(quizId, classroomId);
  const publish = usePublishQuiz(quizId, classroomId);
  const unpublish = useUnpublishQuiz(quizId, classroomId);

  const [dialog, setDialog] = useState<DialogState>(null);
  const [formError, setFormError] = useState<string | null>(null);

  const [availableFrom, setAvailableFrom] = useState<string | null>(null);
  const [availableTo, setAvailableTo] = useState<string | null>(null);
  const [maxAttempts, setMaxAttempts] = useState<string | null>(null);

  // Seed the publish fields from the quiz the first time it loads, then leave them to the teacher.
  const fromValue = availableFrom ?? toInputValue(quiz?.availableFrom ?? null);
  const toValue = availableTo ?? toInputValue(quiz?.availableTo ?? null);
  const attemptsValue = maxAttempts ?? String(quiz?.maxAttempts ?? 1);

  function closeDialog() {
    setDialog(null);
    setFormError(null);
  }

  function failed(fallback: string) {
    return (error: unknown) => {
      setFormError(toUserMessage(error, fallback));
    };
  }

  function handleAdd(input: QuestionInput) {
    setFormError(null);
    addQuestion.mutate(input, {
      onSuccess: () => {
        closeDialog();
        toast.show({ tone: 'success', message: 'Question added.' });
      },
      onError: failed("We couldn't add that question."),
    });
  }

  function handleEdit(questionId: string, input: QuestionInput) {
    setFormError(null);
    editQuestion.mutate(
      { questionId, question: input },
      {
        onSuccess: () => {
          closeDialog();
          toast.show({ tone: 'success', message: 'Question saved.' });
        },
        onError: failed("We couldn't save that question."),
      },
    );
  }

  function handleRemove(questionId: string) {
    removeQuestion.mutate(questionId, {
      onSuccess: () => {
        closeDialog();
        toast.show({ tone: 'success', message: 'Question removed.' });
      },
      onError: (error) => {
        closeDialog();
        toast.show({
          tone: 'danger',
          message: toUserMessage(error, "We couldn't remove that question."),
        });
      },
    });
  }

  function handlePublish() {
    const attempts = Number(attemptsValue);
    publish.mutate(
      {
        availableFrom: fromValue === '' ? null : fromValue,
        availableTo: toValue === '' ? null : toValue,
        maxAttempts: Number.isFinite(attempts) ? attempts : 1,
      },
      {
        onSuccess: () => {
          closeDialog();
          toast.show({ tone: 'success', message: 'Published. Your class can take it now.' });
        },
        onError: (error) => {
          closeDialog();
          toast.show({
            tone: 'danger',
            message: toUserMessage(error, "We couldn't publish that just now."),
          });
        },
      },
    );
  }

  function handleUnpublish() {
    unpublish.mutate(undefined, {
      onSuccess: () => {
        closeDialog();
        toast.show({ tone: 'info', message: 'Taken off the list. Students can no longer start it.' });
      },
      onError: (error) => {
        closeDialog();
        toast.show({
          tone: 'danger',
          message: toUserMessage(error, "We couldn't unpublish that just now."),
        });
      },
    });
  }

  function handleSettingsSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setDialog({ kind: 'publish' });
  }

  if (quizQuery.isPending) {
    return (
      <main className="mx-auto w-full max-w-3xl px-4 py-8">
        <p className="font-body text-text-muted">Loading your quiz…</p>
      </main>
    );
  }

  if (quizQuery.isError) {
    return (
      <main className="mx-auto w-full max-w-3xl px-4 py-8">
        <Card padding="lg">
          <p className="font-body text-text-body">We couldn&rsquo;t load this quiz.</p>
          <Button className="mt-4" onClick={() => void quizQuery.refetch()}>
            Try again
          </Button>
        </Card>
      </main>
    );
  }

  if (quiz === null) {
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

  const busy = addQuestion.isPending || editQuestion.isPending || removeQuestion.isPending;

  return (
    <main className="mx-auto w-full max-w-3xl px-4 py-8">
      <h1 className="font-display text-2xl text-text-strong">{quiz.title}</h1>
      <p className="mt-1 font-body text-text-muted">
        {quiz.isPublished ? 'Published' : 'Draft'} · {quiz.durationMinutes} minutes to finish
      </p>
      <Link
        to={`/classrooms/${quiz.classroomId}/quizzes`}
        className="mt-2 inline-block font-body text-sm text-text-link"
      >
        Back to this class&rsquo;s quizzes
      </Link>

      {quiz.isLocked && (
        <Card padding="lg" className="mt-6">
          <p className="font-body text-text-body">
            Students have started this quiz, so its questions are set now. You can still change when
            it&rsquo;s available and how many attempts it allows.
          </p>
        </Card>
      )}

      {/* Generating is a change to the question set, so it is not offered once the quiz is locked. */}
      {!quiz.isLocked && (
        <div className="mt-6">
          <GenerationSection quizId={quiz.id} classroomId={quiz.classroomId} />
        </div>
      )}

      <Card padding="lg" className={quiz.isLocked ? 'mb-6 mt-6' : 'mb-6'}>
        <div className="mb-3 flex flex-wrap items-center justify-between gap-3">
          <h2 className="font-display text-lg text-text-strong">Questions</h2>
          {!quiz.isLocked && (
            <Button
              onClick={() => {
                setFormError(null);
                setDialog({ kind: 'add' });
              }}
            >
              Add a question
            </Button>
          )}
        </div>

        {quiz.questions.length === 0 ? (
          <p className="font-body text-text-body">
            No questions yet. Add your first one, then you can publish this quiz.
          </p>
        ) : (
          <ul className="flex flex-col">
            {quiz.questions.map((question, index) => (
              <li key={question.id} className="border-b border-border py-3 last:border-b-0">
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div className="max-w-reading">
                    <p className="font-body text-text-strong">
                      {index + 1}. {question.prompt}
                    </p>
                    <p className="mt-1 font-body text-sm text-text-muted">
                      {TYPE_LABEL[question.questionType]} · {pointsLabel(question.points)} ·
                      Answer: {answerSummary(question)}
                    </p>
                  </div>
                  {!quiz.isLocked && (
                    <div className="flex gap-2">
                      <Button
                        variant="secondary"
                        onClick={() => {
                          setFormError(null);
                          setDialog({ kind: 'edit', question });
                        }}
                      >
                        Edit
                      </Button>
                      <Button
                        variant="ghost"
                        onClick={() => setDialog({ kind: 'remove', question })}
                      >
                        Remove
                      </Button>
                    </div>
                  )}
                </div>
              </li>
            ))}
          </ul>
        )}
      </Card>

      <Card padding="lg">
        <h2 className="mb-3 font-display text-lg text-text-strong">When students can take it</h2>
        <form onSubmit={handleSettingsSubmit} noValidate className="flex flex-col gap-4">
          <TextField
            label="Available from"
            type="datetime-local"
            optional
            value={fromValue}
            onChange={(event) => setAvailableFrom(event.target.value)}
            hint="Leave empty to make it available as soon as it's published."
          />
          <TextField
            label="Available until"
            type="datetime-local"
            optional
            value={toValue}
            onChange={(event) => setAvailableTo(event.target.value)}
            hint="Leave empty for no end date."
          />
          <TextField
            label="Attempts allowed"
            type="number"
            min={1}
            required
            value={attemptsValue}
            onChange={(event) => setMaxAttempts(event.target.value)}
          />

          {quiz.questions.length === 0 && (
            <p className="font-body text-sm text-text-muted">
              Add at least one question before publishing.
            </p>
          )}

          <div className="flex flex-wrap gap-2">
            <Button
              type="submit"
              loading={publish.isPending}
              disabled={quiz.questions.length === 0}
            >
              {quiz.isPublished ? 'Update settings' : 'Publish'}
            </Button>
            {quiz.isPublished && (
              <Button
                variant="secondary"
                loading={unpublish.isPending}
                onClick={() => setDialog({ kind: 'unpublish' })}
              >
                Unpublish
              </Button>
            )}
          </div>
        </form>
      </Card>

      <Dialog
        open={dialog?.kind === 'add'}
        onClose={closeDialog}
        title="Add a question"
        size="md"
      >
        <QuestionForm
          submitting={addQuestion.isPending}
          error={formError}
          onSubmit={handleAdd}
          onCancel={closeDialog}
        />
      </Dialog>

      <Dialog
        open={dialog?.kind === 'edit'}
        onClose={closeDialog}
        title="Edit this question"
        size="md"
      >
        {dialog?.kind === 'edit' && (
          <QuestionForm
            initial={dialog.question}
            submitting={editQuestion.isPending}
            error={formError}
            onSubmit={(input) => handleEdit(dialog.question.id, input)}
            onCancel={closeDialog}
          />
        )}
      </Dialog>

      <Dialog
        open={dialog?.kind === 'remove'}
        onClose={closeDialog}
        title="Remove this question?"
        description="It will come off the quiz. You can always add it again."
        tone="danger"
        footer={
          <>
            <Button variant="secondary" onClick={closeDialog}>
              Keep it
            </Button>
            <Button
              variant="danger"
              loading={removeQuestion.isPending}
              onClick={() => {
                if (dialog?.kind === 'remove') handleRemove(dialog.question.id);
              }}
            >
              Remove
            </Button>
          </>
        }
      />

      <Dialog
        open={dialog?.kind === 'publish'}
        onClose={closeDialog}
        title={quiz.isPublished ? 'Update this quiz?' : 'Ready to publish?'}
        description={
          quiz.isPublished
            ? 'Your new availability and attempt settings take effect right away.'
            : 'Everyone in this class will be able to start it, within the times you set.'
        }
        footer={
          <>
            <Button variant="secondary" onClick={closeDialog}>
              Not yet
            </Button>
            <Button loading={publish.isPending} onClick={handlePublish}>
              {quiz.isPublished ? 'Update' : 'Publish'}
            </Button>
          </>
        }
      />

      <Dialog
        open={dialog?.kind === 'unpublish'}
        onClose={closeDialog}
        title="Take this off the list?"
        description="Students won't be able to start it. Anyone partway through keeps their attempt."
        footer={
          <>
            <Button variant="secondary" onClick={closeDialog}>
              Leave it up
            </Button>
            <Button variant="danger" loading={unpublish.isPending} onClick={handleUnpublish}>
              Unpublish
            </Button>
          </>
        }
      />

      {busy && <p className="sr-only" role="status">Saving your change…</p>}
    </main>
  );
}
