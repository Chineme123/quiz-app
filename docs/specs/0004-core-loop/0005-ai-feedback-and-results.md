# 0005. AI feedback and student results (thinnest end to end thread)

**Date**: 2026-07-14

## Summary

This is the first child of the core loop umbrella (0004): the thinnest slice that makes the product's wedge real and visible. A student takes a seeded quiz through the existing take API, the attempt is graded by the existing scoring, and per question feedback on the wrong answers is written by Claude just after grading, then shown on a student results screen. Feedback is generated in the background so submitting stays fast, and it falls back to the deterministic feedback that already exists when Claude is unavailable, so the loop never depends on the model being up. Nothing new is built for classrooms, quiz authoring, or the teacher view in this thread; those are later children.

## Requirements

**User stories**:
- As a student, I want written feedback on the answers I got wrong, so I understand why, not just my score.
- As a student, I want my score the moment I submit, with feedback arriving shortly after, so submitting never feels slow.

**Acceptance criteria**:
- **AC-1**: Submitting an attempt computes and returns the score without waiting for Claude. The submit path does not call the model.
- **AC-2**: After grading, feedback is generated in the background for every incorrect or partial answer in one Claude call per attempt, stored as that answer's feedback with source `Ai`. Each answer in the request carries a local index (1, 2, 3) as a correlation key, and the response is mapped back to answers by that index.
- **AC-3**: Fully correct answers get a short deterministic confirmation with source `Deterministic`, and no model call is made for them.
- **AC-4**: If Claude is unavailable, times out, errors after one retry, or returns a malformed or mismatched response (missing items, or an index or count that does not line up), the deterministic `StandardFeedbackStrategy` writes the feedback for the whole attempt (source `Deterministic`). Feedback always reaches a finished state and the score is never blocked.
- **AC-5**: Only a list of `{ local index, question text, correct answer, the student's submitted answer }` for that one attempt's graded answers is sent to Claude. The index is a per request position (1, 2, 3), not a stored id. No student identity (name, email, `UserId`), no other students' data, and no classroom, teacher, or question identifiers cross the boundary. Feedback is pseudonymous by construction (security.md 2).
- **AC-6**: Prompts, student answers, generated feedback bodies, and the API key are never logged. Only derived feedback text is stored, not raw model transcripts (security.md 2, 6).
- **AC-7**: The attempt reports a `FeedbackStatus` of `Pending` from grading until background generation finishes, then `Ready`.
- **AC-8**: A student reads their own attempt result at `GET /api/attempts/{attemptId}/result`: the score, the feedback status, and a per question breakdown (question text, their answer, the correct answer, correct or not, points, feedback, feedback source).
- **AC-9**: A student can read only their own attempt. A request for an attempt owned by someone else returns `404`. Identity is the `Guid UserId` from the JWT `NameIdentifier` claim, never a client supplied id (security.md 7).
- **AC-10**: Model written feedback is treated as untrusted input: it is length capped and shown as plain text, never rendered as HTML (security.md 1).
- **AC-11**: The dev seeder stands up a teacher, a classroom, a published quiz with a few questions, a student, and an enrolment, so the whole thread runs from a cold `docker compose up`.
- **AC-12**: The student results screen shows the score and the per question breakdown as soon as the attempt is graded. While `FeedbackStatus` is `Pending` it shows a generating state and polls the result endpoint until `Ready`, then renders the feedback (AI and deterministic shown the same way). The calm, supportive, accessible presentation is a design gate against `ui-rules` and the quiztin-design skill, checked in review rather than as a pass or fail test.

## Decision

**Chosen option**: Extend the existing take flow rather than build the ResultService read side now. QuizService generates the feedback (it owns the questions, answers, and attempt) off the graded event, stores it on the answers, and serves the student read directly. The Claude call runs in a background hosted service so submit stays fast, with the deterministic strategy as the fallback on every failure.

**Implementation skills**: `claude-api` (`.claude/skills/claude-api/`, the Anthropic API and current model ids) · `quiztin-design` (`.claude/skills/quiztin-design/`, the results screen tokens and voice)

## Rationale

The wedge is the differentiator and the riskiest, least proven part of the loop, so this first thread goes thin and vertical to prove the whole path (take, grade, real feedback, seen by a human) before widening. Two seams already exist and shape the design: `QuizAnswer.Feedback` is a field the strategy fills, and `IFeedbackStrategy.Generate` is written for the future AI strategy, so the AI path is a new strategy behind an existing seam, not new plumbing. Serving the read from QuizService (which already loads the full attempt) avoids standing up the cross service ResultService projection, which is the heaviest single piece and is not the wedge. The background service keeps Claude latency off the submit path, which matters because per question feedback is high volume and the loop must never wait on the model. Full reasoning and the options weighed are in the umbrella `rationale.md`.

## Feature design

**Data model sketch** (deltas only; everything else exists):
- `QuizAttempt` (exists): add `FeedbackStatus` (`Pending` then `Ready`, not null, default `Pending`) and `FeedbackGeneratedAt` (`DateTime?`, nullable). Has many `QuizAnswer`.
- `QuizAnswer` (exists): `Feedback` (`string?`) already present; add `FeedbackSource` (`Ai` or `Deterministic`, nullable until feedback is written).
- `Question` (exists, read only here): carries the correct answers the feedback references.
- No new tables. No ResultService read model this thread.

**State transitions**: `NotStarted` then `InProgress` then `Submitted` then `Graded` then `Reviewable` (plus terminal `Abandoned`). Submit now leaves the attempt at `Graded` with the score set and `FeedbackStatus` `Pending`, and dispatches the graded event. The background job writes feedback, sets `FeedbackStatus` `Ready`, records `FeedbackGeneratedAt`, and moves the attempt to `Reviewable`. The result read works from `Graded` onward (score visible while feedback is still `Pending`).

**API surface**:
| Endpoint | Method | Key inputs | Key outputs | Auth | Key errors |
|---|---|---|---|---|---|
| /api/attempts/{attemptId}/result | GET | attemptId (path) | score, feedbackStatus, per answer breakdown | bearer (student) | 404 not found or not owner |

Existing endpoints are unchanged in shape: start attempt and submit attempt keep working, except submit no longer generates feedback inline (it dispatches the graded event instead).

**Key invariants**:
- Submit never calls Claude and never blocks on it (AC-1).
- Every attempt reaches `FeedbackStatus` `Ready` even during a Claude outage, because the fallback always finishes the job (AC-4, AC-7).
- The only academic content that leaves the service is `{ question, correct answer, student answer }` per graded answer, with no identity (AC-5).
- A student can read only attempts where `attempt.StudentId` equals the caller's `Guid UserId` (AC-9).
- Stored feedback is derived text only, length capped, shown as plain text (AC-6, AC-10).
- Feedback generation is idempotent per attempt: a duplicate or redelivered graded event does not regenerate feedback or throw. The background job skips an attempt whose `FeedbackStatus` is already `Ready` (or that is past `Graded`), so a redelivery is a safe no op (AC-2, AC-7).
- The background job processes each attempt in its own service scope (a fresh scoped `DbContext` and repository), and one failing attempt is caught, logged without content, and skipped, never crashing the hosted service.

**Security model**: The student reads only their own attempt, scoped by the `Guid UserId` from the token, never a client id. The Claude payload is minimized and pseudonymous per security.md 2 (the model never learns who answered). The Claude API key lives only in QuizService config (secrets or env), never in the other services and never sent to the frontend (security.md 3). No prompt, answer, feedback body, or key is logged (security.md 6). Student academic performance stays inside the classroom boundary (security.md 7). This thread handles no other regulated data.

**Configuration required**:
- `Anthropic__ApiKey`: the Claude API key, from user secrets or env, QuizService only.
- `Anthropic__FeedbackModel`: the model id, default `claude-haiku-4-5` (a fast, cost effective tier; confirm the current id via the claude-api skill at build time).
- `Anthropic__TimeoutSeconds`: per call timeout before the fallback, default `10`.
- `Feedback__AiEnabled`: feature flag. When false or when no key is present, the deterministic strategy is used for everything, so the loop builds and runs before the key is provisioned.

**Critical test scenarios**:
- Happy path: a student submits, gets the score at once, and within a moment the results screen shows Claude feedback on the wrong answers, verifies **AC-1**, **AC-2**, **AC-8**, **AC-12**.
- Failure case: Claude times out; the deterministic fallback fills the feedback, the attempt reaches `Ready`, source is `Deterministic`, verifies **AC-4**.
- Data minimization: the request payload to Claude contains only question, correct answer, and student answer, and no identity, verifies **AC-5**, **AC-6**.
- Auth: a student requests another student's attempt result and receives `404`, verifies **AC-9**.
- Malformed model output: Claude returns `200` but with fewer or misaligned items than requested; the whole attempt falls back to deterministic feedback rather than mis attaching, verifies **AC-4**.
- Duplicate event: the graded event is delivered twice; the second run is a no op and neither throws nor calls Claude again, verifies **AC-2**, **AC-7**.

## Build plan

The order is a thin vertical thread through every layer, then thicken. This is a deliberate local exception to the breadth first stance in foundation 0, chosen because this first child exists to prove the wedge end to end and reduce the AI risk before the umbrella widens.

1. Domain first: add the `FeedbackStatus` (`Pending`, `Ready`) and `FeedbackSource` (`Ai`, `Deterministic`) enums; add `FeedbackStatus` and `FeedbackGeneratedAt` to `QuizAttempt` and `FeedbackSource` to `QuizAnswer`; `QuizAttempt` sets `Pending` at grade and, after feedback, a guarded move to `Ready` and `Reviewable` that is skipped when it is already `Ready`, satisfies **AC-7**, **AC-2**, **AC-3**.
2. Migration: generate the EF migration from the changed model (the two new columns). The model change comes first because EF generates a migration by diffing the current entities, so this step cannot precede step 1, satisfies **AC-7**.
3. Take the feedback call off the submit path: remove the inline `GenerateFeedback` from `TakeQuizFacade.SubmitQuizAsync`; submit ends `Graded` and `Pending` and dispatches the graded event, satisfies **AC-1**.
4. Config and secrets: `Anthropic__ApiKey` from secrets, `Anthropic__FeedbackModel` (default `claude-haiku-4-5`), `Anthropic__TimeoutSeconds` (default `10`), and the `Feedback__AiEnabled` flag. This lands before the strategy that reads it; when disabled or keyless, everything uses the deterministic strategy, so the loop builds and runs before the key exists, satisfies **AC-4**.
5. AI feedback strategy behind `IFeedbackStrategy`, using the Anthropic .NET SDK: build the minimized payload (one list of `{ local index, question, correct answer, student answer }` for the incorrect and partial answers); one call per attempt; require the structured response to echo each index and map feedback back by it; on a missing, extra, or misaligned item, or any error after one retry inside the short timeout, fall back to `StandardFeedbackStrategy` for the whole attempt with source `Deterministic`; correct answers get the deterministic confirmation with no call; cap the feedback length and treat it as untrusted (plain text, never HTML), satisfies **AC-2**, **AC-3**, **AC-4**, **AC-5**, **AC-6**, **AC-10**.
6. Background generation: a channel enqueued from the post commit graded event, and a hosted `FeedbackGenerationService` that, per dequeued attempt, opens an `IServiceScopeFactory` scope for a fresh scoped `DbContext` and repository, skips the attempt when `FeedbackStatus` is already `Ready` (idempotent under a redelivered event), runs the feedback strategy, saves, and sets `Ready`. Each attempt is wrapped in its own try and catch, so a bad response or a concurrency error (`DbUpdateConcurrencyException` off the `xmin` token) is logged without content and skipped, never letting the default host behavior stop the service, satisfies **AC-2**, **AC-7**.
7. Read endpoint: `GET /api/attempts/{attemptId}/result` on QuizService, scoped to the caller's `Guid UserId` (404 otherwise), returning the attempt result DTO, satisfies **AC-8**, **AC-9**.
8. Seeder: extend `DataSeeder` to create a teacher, a classroom, a published quiz with questions, a student, and an enrolment, satisfies **AC-11**.
9. Student results screen in the SPA: score header plus the per question breakdown; a generating state that polls the result endpoint until `Ready`; feedback shown as plain text; the calm, supportive, accessible presentation follows `ui-rules` and the quiztin-design skill, satisfies **AC-12**, **AC-8**.

## Consequences

**Positive**:
- The wedge becomes real and demoable from a cold start, and the landing page's AI claim becomes true.
- Submit stays fast, and a Claude outage degrades to deterministic feedback instead of breaking the loop.
- The AI integration and the background async pattern are proven once here and reused by the other children (quiz generation, the ResultService projection).

**Negative / tradeoffs**:
- Background feedback has no durable queue (no outbox in v1, foundation 9 and 10). If the process restarts, or the enqueue is lost after the commit, the attempt stays `Pending` indefinitely, and because submit is idempotent on the `CommandId` a client retry is a silent no op. There is no built in recovery this pass (Follow-up).
- The Anthropic .NET SDK adds a dependency, against the few dependencies house style (foundation 7 #21). Chosen deliberately by the engineer.
- The UC9 read lives in QuizService, a temporary deviation from the ResultService read side split (foundation 7 #8), to be moved when the ResultService child is built.
- Polling adds a few extra requests per results view until feedback lands.

**Neutral**:
- A new Claude API key secret enters QuizService config only.
- The dev seeder now carries the loop preconditions.
- A new hosted background service runs inside QuizService.

## Follow-up

- [ ] Move the UC9 read to ResultService with the graded event projection when that child is built, restoring foundation 7 #8.
- [ ] Add a re-generate trigger (or a light outbox) so feedback lost on a restart can recover; today the attempt stays `Pending`.
- [ ] Record the Anthropic .NET SDK (`Anthropic.SDK`) as an approved dependency in `library-docs.md`, and note the foundation 7 #21 exception.
- [ ] Reconcile the stale drift: foundation 10 and `build-graph.md` still call scoring fake, but the scoring contract is redesigned and real (`IScoringStrategy.Score(attempt, questions)`, `PointsScoringStrategy`).
- [ ] Add a recovery path for feedback stuck `Pending` (a lazy re trigger when the read finds `Pending` older than a threshold, a manual regenerate action, or a light outbox); today a lost enqueue strands the attempt.
- [ ] Add a unique index on `ProcessedCommand.CommandId` (`QuizDbContext` defines none today). The check then write in `SubmitQuizAsync` is a race between two concurrent submits, and 0005 raises its cost from a harmless duplicate to two feedback jobs and two Claude calls racing on one attempt.

## Rationale

Reasoning and the options weighed live in the umbrella [rationale.md](rationale.md).
