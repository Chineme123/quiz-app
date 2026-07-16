# 0006. Take quiz screen (the student take experience)

**Date**: 2026-07-16

## Summary

This is the child of the core loop umbrella (0004) that lets a real person take a quiz. Today a student can only be graded by curl or the seeder, because there is no screen. This adds a list of the quizzes available to a student, a take screen that shows one question at a time with a navigator grid and a countdown, and answers that save to the server as they are picked, so a refresh or a dropped connection never costs a student their work. Submitting grades the answers already saved and hands off to the results screen that 0005 already built, which closes the loop end to end for a human.

Two decisions here deliberately override `foundation.md`, which is the source of truth, so both carry follow ups to update it: in progress auto save comes into v1 (deferred in section 8), and running out of time grades the saved answers instead of abandoning them (trigger 1 in section 69). Triggers 2, 3, and 4 stay as locked, and no background sweeper is added.

## Requirements

**User stories**:
- As a student, I want to see which quizzes I can take and start one, so I do not need someone to hand me a link.
- As a student, I want my answers saved as I pick them, so a refresh, a closed tab, or a flaky connection never destroys my only attempt.
- As a student, I want to see how much time is left and move between questions freely, so I can pace myself and check my work before I submit.

**Acceptance criteria**:

- **AC-1**: A student sees a list of the quizzes available to them: published, inside their availability window, and only in classrooms they are enrolled in. The list is scoped to the authenticated `Guid UserId` from the JWT `NameIdentifier` claim, never a client supplied id. It is paginated with a default page size and a hard cap.
- **AC-2**: Each row shows the right action for its state: **Start** when there is no attempt, **Resume** (carrying the open `attemptId`) when an attempt is in progress, and **View result** linking to `/results/{attemptId}` when the attempt is graded. The list read supplies the open `attemptId`, so no extra round trip is needed and a second attempt cannot be started by accident.
- **AC-3**: Starting a quiz creates the attempt and pins `ExpiresAt = StartedAt + Quiz.DurationMinutes` on it at that moment. Nothing ever recalculates it, so a teacher editing `DurationMinutes` cannot move the clock on an attempt already running. Enrolment still gates starting and the attempt limit is still checked (both already exist in `TakeQuizFacade.StartQuizAsync` and `Quiz.CanStart`).
- **AC-4**: The take screen reads its questions from `GET /api/attempts/{attemptId}/questions`, scoped to the attempt's owner. The response never contains a correct answer, only `{ id, questionType, prompt, points, options }` per question, plus the attempt's `expiresAt`, the server's `serverNow`, and the saved draft answers.
- **AC-5**: A request for an attempt that belongs to someone else, or that does not exist, returns `404` from every attempt scoped endpoint (questions, draft save, submit, result). It never reveals that the attempt exists.
- **AC-6**: Picking or editing an answer saves to the server, debounced by about 500ms, via `PUT /api/attempts/{attemptId}/answers`, which **replaces the whole draft set in one write** (last write wins). The client sends every answer it holds, so the server never reads then modifies then writes, and two saves in flight can never interleave and silently drop an answer.
- **AC-7**: A draft save is rejected with `409` once `now > ExpiresAt`, or if the attempt is not `InProgress`. The deadline is enforced by the server, not only counted down by the client, so a student who ignores or blocks the client clock still cannot keep working past their time.
- **AC-8**: The screen shows a quiet save state (saving, saved, retrying). A failed save keeps the answer on screen and retries in the background; a failure that persists escalates to a clear warning, so a student is never told their work is safe when it is not.
- **AC-9**: Returning to an in progress attempt (refresh, or Resume from the list) restores every answer already saved, and the countdown continues against the same pinned `ExpiresAt`.
- **AC-10**: The countdown is computed from `expiresAt` and the `serverNow` the server sends with it, so a device with a wrong clock still shows the true remaining time. At zero the client submits automatically.
- **AC-11**: Submitting sends only a `CommandId`. The server reads the saved draft answers, writes them as this attempt's `QuizAnswer` rows, and grades those. The body carries no answers, so the saved drafts are the single source of truth and cannot disagree with a request body. Submitting twice is still idempotent and returns the existing result (the behaviour 0005's follow up fix established), and caller scoping stays exactly as it is.
- **AC-12**: A submit that arrives after `ExpiresAt` grades the saved answers rather than rejecting them or abandoning the attempt. Since drafts stop being writable at `ExpiresAt` (AC-7), what is graded is exactly what the student had saved when their time ran out. **This overrides `foundation.md` section 69 trigger 1** and carries a follow up to update it.
- **AC-13**: Submitting with questions still unanswered shows a confirm dialog naming how many are blank, and proceeds only if the student confirms.
- **AC-14**: `Abandoned` triggers 2, 3, and 4 behave as `foundation.md` section 69 locks them, with these clarifications this spec settles:
  - **Trigger 2** (availability window close) gates **starting only**. A student already inside a quiz may finish and submit it even if the window closes mid attempt: the window governs whether you can start, not whether you can finish. It is therefore never evaluated on a draft save or a submit, so auto save can never abandon an attempt mid keystroke.
  - **Trigger 3** (explicit quit) exists as a domain action, but the take screen exposes no quit button in v1.
  - **Trigger 4** (superseded) abandons a prior unfinished attempt when a new one starts.
  - No background sweeper is added, per the same locked decision.
- **AC-15**: Attempts abandoned by **trigger 4 (superseded) do count** toward `Quiz.MaxAttempts`; attempts abandoned by trigger 2 or trigger 3 do not. Restarting is the only abandon a student controls, so it is the only one that costs them, which stops a student starting repeatedly to read the questions for free on a `MaxAttempts = 1` quiz. An attempt that is merely `InProgress` does not consume the limit; only `Submitted`, `Graded`, `Reviewable`, and superseded `Abandoned` attempts do.
- **AC-16**: Submitting an attempt that is already `Abandoned` (for example a stale tab auto submitting after a supersede) returns a clear, handled response saying the attempt was superseded. It never throws an unhandled state error out of the domain as a raw `400`.
- **AC-17**: The take screen shows one question at a time with a navigator grid that marks answered, unanswered, and current, and allows jumping to any question. A sticky header carries the quiz title, the countdown, and the save state. All three question types render: multiple choice and true or false as radio groups, short answer as a text input.
- **AC-18**: Submitting navigates to `/results/{attemptId}`, the screen 0005 already built. Grading still raises `QuizAttemptGradedEvent`, so the existing feedback pipeline runs untouched.
- **AC-19**: The screen is keyboard operable and screen reader sane: the navigator is a real list of buttons with state announced, the countdown updates politely rather than interrupting, and the question input is properly labelled. Verified with `vitest-axe` alongside the existing suite.

## Decision

**Chosen option**: build the take experience on the existing attempt API, adding a take scoped questions read, a whole set draft save into one `jsonb` column on the attempt, and a server pinned deadline. The saved drafts are the single source of truth for grading; submit reads them rather than a request body.

Drafts live in a single `DraftAnswersJson` column on `QuizAttempt`, replaced wholesale on each save, rather than as live `QuizAnswer` rows. `QuizAnswer` keeps its current shape and is still written once, at submit, by the path that already works. This follows the `jsonb` precedent already in the codebase (`MultipleChoiceQuestion.Options`).

The take screen never reads `GET /api/quizzes/{quizId}`. That endpoint is `[Authorize]` but otherwise unscoped, so any signed in user can read any quiz by id; rather than widen it, this child adds `GET /api/attempts/{attemptId}/questions`, where the attempt itself is the authorisation (already owned by one student, enrolment already checked at start). The unscoped teacher read is recorded as a follow up.

**Implementation skills**: `quiztin-design` (`.claude/skills/quiztin-design/`), the design source for this screen, together with the design tokens and the patterns the 0005 results screen already set.

## Rationale

The two overrides earn their cost. `MaxAttempts` defaults to `1`, so a quiz is normally a single shot: a design where a refresh loses the work, or where a timeout throws away answers the server is already holding, is punishing in a product whose whole point is helping a student learn. Saving answers as they are picked makes the timer safe rather than cruel, and once the answers are on the server, grading them at expiry costs nothing and abandoning them is simply unkind. Both changes are narrow: section 8 gains auto save, section 69 loses only trigger 1.

The draft blob was chosen over live `QuizAnswer` rows after a cross check found the rows carried two real costs the blob does not. Rows would have needed `IsCorrect` to become nullable so an ungraded draft could not read as "wrong", which ripples into shipped 0005 code (`StandardFeedbackStrategy`, `AiFeedbackStrategy`, `AttemptResultDto`, and their tests); and a per question upsert has a genuine race, where two concurrent first writes both insert and the unique index makes the loser throw rather than merge. Replacing one column wholesale has neither problem, needs no new index, and leaves `QuizAnswer` and the 0005 code completely untouched. The cost is that drafts are not queryable, which only matters for a live teacher view that does not exist and is not in this child.

Grading the saved drafts rather than a request body is what makes the rest coherent. The normal path and the expiry path become the same path (write the saved answers as rows, then grade), so there is one grading path to reason about and test. It does change the submit contract, in a method only just fixed for idempotency and caller scoping, so that method needs tests rather than confidence.

Storing `ExpiresAt` looks like storing a derived value, which is normally wrong. It is justified because the value it derives from is mutable: a teacher editing `DurationMinutes` mid attempt would otherwise silently move the clock on a running student. Pinning it at start makes the deadline a contract rather than a calculation, and gives the client and server one instant to agree on. Enforcing it on the draft write (AC-7) is what keeps the countdown honest rather than decorative.

## Feature design

**Data model sketch** (no new entities, no new tables, no new indexes):

| Entity | Change |
|---|---|
| `QuizAttempt` | **new** `ExpiresAt: DateTime` (not null), pinned at `Start(durationMinutes)` to `StartedAt + Quiz.DurationMinutes`, never recalculated. **new** `DraftAnswers` (`jsonb`, not null, defaults to `{}`), the in progress answers as a map of `questionId` to the answer string, replaced whole on every save and cleared when the attempt leaves `InProgress`. **new** `AbandonReason: AbandonReason?` (nullable, stored as text like `CurrentStateName`), null unless the attempt is `Abandoned`. |
| `QuizAnswer` | **unchanged.** No mutability, no nullable `IsCorrect`, no new index. Rows are still created once, at submit, from the draft blob, by the path that already works. |

`AbandonReason` is what makes AC-15 enforceable: the attempt limit treats the reasons differently, so the reason has to be on the row. Its values are `Superseded` and `Quit` only. Trigger 1 is absent because expiry no longer abandons (AC-12), and trigger 2 is absent because the window gates starting rather than abandoning a running attempt (AC-14).

Relationships are unchanged: `Classroom 1:N Quiz`, `Quiz 1:N Question`, `Quiz 1:N QuizAttempt`, `QuizAttempt 1:N QuizAnswer`, `Question 1:N QuizAnswer`, `Enrollment` unique on `(StudentId, ClassroomId)`.

**State transitions**:

`NotStarted → InProgress → Submitted → Graded → Reviewable`, plus terminal `Abandoned`, as foundation section 69 locks it, except trigger 1.

- `InProgress` holds **no** `QuizAnswer` rows. The answers live in `DraftAnswersJson` until submit, so the graded record stays clean and provisional work is obviously provisional.
- `Submit()` takes no answers. It reads `DraftAnswersJson`, writes the `QuizAnswer` rows, then grades them.
- `Abandoned` is reached by: **trigger 1** expiry, **which no longer abandons; the attempt grades its saved answers instead** (this spec's override); **trigger 2** availability window close, evaluated on **start only**; **trigger 3** explicit quit, eager, domain only in v1; **trigger 4** superseded, when a new attempt starts over an unfinished one.
- The count `Quiz.CanStart` compares against `MaxAttempts` includes `Submitted`, `Graded`, `Reviewable`, and attempts abandoned by trigger 4. It excludes `InProgress` and attempts abandoned by triggers 2 and 3.

**API surface**:

| Endpoint | Method | Key inputs | Key outputs | Auth | Key errors |
|---|---|---|---|---|---|
| `/api/quizzes/available` | GET | `page:int` (opt), `pageSize:int` (opt, capped) | `{ items:[{ quizId, title, durationMinutes, questionCount, state, attemptId? }], total }` | bearer (student) | 401 |
| `/api/quizzes/{quizId}/start` | POST | `quizId:Guid` (req) | `{ attemptId }` | bearer (student) | 400 not enrolled / limit reached / window closed |
| `/api/attempts/{attemptId}/questions` | GET | `attemptId:Guid` (req) | `{ quizTitle, expiresAt, serverNow, draftAnswers, questions:[{ id, questionType, prompt, points, options }] }` | bearer (owner) | 404 not owner / not found |
| `/api/attempts/{attemptId}/answers` | PUT | `answers: map<Guid,string>` (req), replaces the whole set | `204` | bearer (owner) | 404 not owner, 409 not `InProgress` or past `ExpiresAt` |
| `/api/attempts/{attemptId}/submit` | POST | `commandId:Guid` (req) | graded result (score + breakdown) | bearer (owner) | 404 not owner, 409 attempt superseded |
| `/api/attempts/{attemptId}/result` | GET | `attemptId:Guid` (req) | 0005's result payload | bearer (owner) | 404 not owner |

**Key invariants**:
- `ExpiresAt` is immutable once set; nothing recalculates it, and editing a quiz's `DurationMinutes` never moves a running attempt's deadline.
- No draft write is accepted once `now > ExpiresAt`, so what is graded at expiry is exactly what was saved when time ran out.
- The draft blob is replaced whole on every save. The server never reads then modifies then writes it, so concurrent saves cannot interleave and drop an answer; the last write simply wins.
- A `QuizAttempt` has `QuizAnswer` rows only from `Submitted` onward. An answer row always means a submitted answer, never a draft, so nothing can misread provisional work as graded.
- Grading reads only the persisted drafts. The submit body carries no answers at all.
- The server never sends a correct answer to a student before their attempt is graded; the take payload is `{ id, questionType, prompt, points, options }` by construction, which is already what `QuestionDto` exposes.
- Every attempt scoped read and write resolves identity from the JWT `NameIdentifier` claim and returns `404`, never `403`, for another student's attempt (umbrella contract 3, `security.md` 4 and 7).

**Security model**:
- **Student** may: list quizzes they are enrolled in that are published and in window; start an attempt on those; read questions, save drafts, submit, and read the result **for their own attempts only**. Ownership is the attempt's `StudentId` compared to the JWT identity.
- **Nobody** reads a correct answer through the take path before grading.
- The take screen deliberately does **not** use `GET /api/quizzes/{quizId}`, which is currently unscoped; see Follow up.
- Repeated starts cannot be used to read a quiz for free: a superseded attempt consumes an attempt (AC-15).
- No new regulated data and no new PII. The AI boundary is untouched: submit still raises `QuizAttemptGradedEvent` and 0005's pipeline still sends only `{ question, correct answer, student answer }`, so umbrella contract 1 holds unchanged.

**Configuration required**: none. This feature adds no environment variables, no secrets, and no third party credentials.

**Critical test scenarios** (each maps to an acceptance criterion):
- Happy path: a seeded, enrolled student opens the list, starts the quiz, answers all three question types with each save landing, and submits; the attempt is graded and lands on `/results/{attemptId}`, verifies **AC-1, AC-2, AC-3, AC-6, AC-11, AC-17, AC-18**.
- Resume: a student answers two questions, reloads, and both answers come back with the countdown still against the original `ExpiresAt`, verifies **AC-9, AC-10**.
- Deadline is real: a draft save attempted after `ExpiresAt` is rejected with `409`, verifies **AC-7**.
- Expiry grades what was saved: a submit arriving after `ExpiresAt` grades the saved answers rather than rejecting or abandoning them, verifies **AC-12**.
- Window close does not abandon: the availability window closes while a student is mid attempt; draft saves keep working and the submit still grades, verifies **AC-14**.
- Farming is closed: starting again abandons the first attempt by supersede AND consumes the `MaxAttempts = 1` allowance, so a third start is refused, verifies **AC-15**.
- Superseded submit: a stale tab auto submitting an attempt already abandoned by supersede gets a handled "superseded" response, not a raw `400`, verifies **AC-16**.
- Auth/permission: student B requests student A's questions, draft save, and submit, and receives `404` from each, verifies **AC-5**.
- Accessibility: the take screen passes `vitest-axe`, and the navigator is operable by keyboard alone, verifies **AC-19**.

## Build plan

Ordered breadth first, per `foundation.md` section 0 and the umbrella's note that it returns to breadth first after 0005's deliberate thin exception: the whole server surface is completed before the screen is built on it. The domain change comes before the migration, because EF generates a migration by diffing the current entities, so the entities must change first (the same order 0005's build plan established).

1. Domain: pin `ExpiresAt` in `Start()` (it needs the quiz's `DurationMinutes`); add `DraftAnswersJson`; change `Submit()` to take no answers, write the `QuizAnswer` rows from the draft blob, then grade them, satisfies **AC-3, AC-6, AC-11**
2. Domain: the `Abandoned` triggers as locked, minus trigger 1 (expiry grades the saved answers); trigger 2 gates start only; trigger 3 as a domain action with no UI; trigger 4 supersede. Count `Submitted`, `Graded`, `Reviewable`, and supersede abandoned attempts toward `MaxAttempts`, and exclude `InProgress` and triggers 2 and 3, satisfies **AC-12, AC-14, AC-15**
3. Migration generated from the entities above: `QuizAttempt.ExpiresAt` and `QuizAttempt.DraftAnswersJson` (`jsonb`), satisfies **AC-3, AC-6**
4. `GET /api/attempts/{attemptId}/questions`, attempt scoped, returning `expiresAt`, `serverNow`, the saved drafts, and no correct answers, satisfies **AC-4, AC-5, AC-9**
5. `PUT /api/attempts/{attemptId}/answers`, a whole set replace, rejected with `409` when not `InProgress` or past `ExpiresAt`, satisfies **AC-6, AC-7**
6. Change submit: body carries only `CommandId`; grading reads the persisted drafts; a late submit grades rather than rejects; a superseded attempt returns a handled `409`. Keep the existing idempotency and caller scoping intact and covered, satisfies **AC-11, AC-12, AC-16**
7. `GET /api/quizzes/available`, scoped to enrolment, published, in window, paginated, carrying per quiz state and the open `attemptId`, satisfies **AC-1, AC-2**
8. Backend tests: the scoping `404`s, the post deadline `409`, the late submit grading, the window close not abandoning a running attempt, the supersede consuming the limit, and the superseded submit response, satisfies **AC-5, AC-7, AC-12, AC-14, AC-15, AC-16**
9. Frontend: the available quizzes list page with its Start, Resume, and View result states, satisfies **AC-1, AC-2**
10. Frontend: the take screen shell (route, sticky header, question card, navigator grid, prev and next) with all three question inputs, satisfies **AC-17**
11. Frontend: debounced whole set auto save with the quiet save state and background retry, satisfies **AC-6, AC-8**
12. Frontend: resume restoring saved answers, and the countdown on the `serverNow` offset with auto submit at zero, satisfies **AC-9, AC-10**
13. Frontend: submit with the unanswered confirm dialog, then navigate to `/results/{attemptId}`, satisfies **AC-13, AC-18**
14. Frontend tests including `vitest-axe`, and the design gate against `quiztin-design` and the tokens, satisfies **AC-17, AC-19**

## Consequences

**Positive**:
- The loop finally closes for a human: a student can find a quiz, take it, and read AI feedback without curl or the seeder. This is the child that makes the other four demonstrable.
- The take screen never touches the unscoped quiz read, so the feature ships without leaning on a known security gap.
- Saved answers make a `MaxAttempts = 1` timed quiz forgiving: a refresh, a dead battery, or a late submit no longer destroys a student's only attempt.
- The draft blob leaves `QuizAnswer` and all the shipped 0005 feedback code completely untouched, and needs no new index and no upsert race handling.
- Normal and expiry paths collapse into one grading path, so there is a single thing to reason about and test.

**Negative / tradeoffs**:
- Two locked foundation decisions are overridden (section 8 auto save, section 69 trigger 1). Overriding the source of truth is a real cost, and it is only honest if the follow ups below actually land.
- Submit's contract changes, in a method only just fixed for idempotency and caller scoping. That is the highest risk edit in this child and needs tests before confidence.
- Drafts are not queryable while in progress, because they are one opaque `jsonb` column. A live teacher view of in progress attempts would need real rows and is not possible on this shape.
- Whole set replace sends every answer on every debounced save, not just the one that changed. At a handful of questions this is trivial; on a very long quiz it would not be.
- Last write wins across two tabs: the tab that saves last silently wins. Acceptable because both tabs are the same student, but it is a real behaviour, not an accident.
- A student who simply closes their laptop leaves the attempt `InProgress` until they return, because no sweeper is added (as locked). It grades their saved answers whenever they come back.

**Neutral**:
- No new dependencies: the debounce is hand rolled per the few dependencies house style (section 7 #21), and the timer is a small hook rather than a library.
- No new environment variables, secrets, or services.
- The `jsonb` column follows the precedent already set by `MultipleChoiceQuestion.Options`, including its `ValueComparer` handling.
- The wizard and navigator are more frontend state than a single scrolling form, which is the cost of the exam like experience chosen here.

## Follow-up

- [x] **Update `foundation.md` section 8**: done (v3.1). "Save-Draft; in-progress auto-save" is struck from the deferred list with the reason, and a `progress-log.md` docs entry records the override.
- [x] **Update `foundation.md` section 69**: done (v3.1). Trigger 1 no longer abandons; it grades the saved answers. Trigger 2 is clarified as gating starting only. Which abandons consume an attempt is recorded (only trigger 4). "No background sweeper" stays locked and unchanged.
- [ ] **Fix the unscoped `GET /api/quizzes/{quizId}`** (`QuizController.cs`): it is `[Authorize]` but takes no identity, so any signed in user can read any quiz by id. This child routes around it rather than depending on it, but it is a live violation of umbrella contract 3 and should be scoped to the owning teacher.
- [x] Remove the dead first enrolment check in `TakeQuizFacade.StartQuizAsync`: done inline while pinning `ExpiresAt`, since the same method was already being edited.
- [ ] **Decide what to do with `SubmitQuizCommand`.** `code-standards.md` section 6 already called it a hollow passthrough; dropping its answers payload leaves it wrapping a zero argument call, so the choice between giving it real behaviour and removing it is now sharper. Surfaced during the build, out of scope for it.
- [ ] **Snapshot the question set at start**, or decide not to. Only the deadline is pinned today, so a quiz edited mid attempt changes the question list under a student who resumes. `PointsScoringStrategy` already silently scores zero for an answer whose question is not found, so this would mask rather than surface. Out of scope here; it needs its own decision.
- [ ] Decide whether the teacher needs a live view of in progress attempts. The draft blob cannot serve it (not queryable), so it would need real rows; it belongs with the ResultService child (UC10), not here.
