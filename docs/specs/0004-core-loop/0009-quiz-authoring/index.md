# 0009. Quiz authoring, AI generation, and publish

**Date**: 2026-07-20

## Summary

This is the child of the core loop umbrella (0004) that lets a teacher build and publish a quiz through the product, and makes the AI generation seam real. Today a teacher can create a quiz and add questions by API, but there is no way to publish one, so a quiz never reaches a student; the only takeable quiz is the seeded one. This slice closes that gap and adds the whole authoring experience on top of it: a publish step that sets the availability window and attempt limit and flips the quiz live, manual add, edit, and delete of questions, real Claude question generation from a topic (with pasted or uploaded source material), a server saved draft the teacher reviews before anything joins the quiz, and the SPA screens that drive all of it. With this in, a teacher goes from an empty classroom to a live, student takeable quiz without touching the seeder, and the loop the umbrella describes (create, take, results, feedback) is finally walkable end to end by real people on both sides.

Generation reaches Claude through the same community `Anthropic.SDK` and flag plus key gate the feedback path already uses (spec 0005), with `claude-opus-4-8` for the higher stakes generation call. Because the community SDK does not constrain output to a schema, the model's questions are validated by hand against the question rules before anything is stored, one candidate at a time, so a partly bad batch yields its good questions rather than failing whole. Every model call keeps a deterministic fallback: with AI off or the call failing, the teacher gets empty editable question templates instead of nothing.

## Requirements

**User stories**:
- As a teacher, I want to publish a quiz with an availability window and an attempt limit, so my enrolled students can actually take it.
- As a teacher, I want to build a quiz by hand, adding, editing, and removing questions of each type, so I control exactly what my students see.
- As a teacher, I want Claude to draft questions from a topic and my own notes, so I do not start from a blank page, while I stay the one who decides what goes in.
- As a teacher, I want the questions Claude drafts to wait in a review step I can come back to, so a refresh or a second look never loses them and nothing reaches students unread.

**Acceptance criteria** (the contract, each IDed and independently checkable):

- **AC-1**: A teacher can publish a quiz they own. Publishing sets the availability window (`AvailableFrom` and `AvailableTo`, each optional) and `MaxAttempts` (at least 1), then sets `IsPublished` true. It is refused with 400 if the quiz has zero questions, refused with 400 if both window bounds are set and `AvailableFrom` is not before `AvailableTo`, and refused with 404 if the caller does not own the quiz. Ownership is the caller's `Guid UserId` from the JWT `NameIdentifier` equalling the quiz's `CreatedByTeacherId`.
- **AC-2**: A published quiz becomes takeable for its enrolled students within its window with no further wiring: the existing `Quiz.CanStart` and available quizzes read already consume `IsPublished`, the window, and `MaxAttempts`, and publish is the first and only writer of those fields. Unpublishing sets `IsPublished` false and removes the quiz from students' available lists at once.
- **AC-3**: A teacher can create a quiz in a classroom they own, and add, edit, and delete its questions by hand (multiple choice, true or false, and short answer), each validated by the same `QuestionFactory` rules a generated question is. A non owner attempting any of these gets 404.
- **AC-4**: A teacher can generate candidate questions for a quiz they own from a topic, a difficulty, and a requested count (capped). With AI enabled and an API key present, the questions come from `claude-opus-4-8`; with AI disabled, or the model call failing or timing out, the teacher instead gets the same requested number of empty editable question templates. Generation always returns something the teacher can work with (the deterministic fallback, security.md section 1).
- **AC-5**: Only task necessary academic content reaches the model: the topic, the difficulty, the count, and any source material the teacher attached, and nothing else. No student data, no classroom or teacher identifiers, no other quiz's content (security.md section 2). Prompts, model responses, and the API key are never logged (security.md section 6).
- **AC-6**: Model output is untrusted and validated before anything is persisted, at two layers, so a single bad element never fails the batch. First, the model's JSON array is parsed **element by element** (tolerantly), not as one strict deserialize, so one candidate with a wrong field type is a single failed candidate, not a failure of the whole array (the feedback path's one shot `Deserialize<List<T>>` fails whole on one bad element, which is fine for its whole attempt fallback but would break the subset promise here). Second, each parsed candidate is validated against the question rules before it is constructed: a non blank prompt, points above zero, and per type (a multiple choice question has at least 2 options and an in range correct index; a short answer has non blank correct text; a true or false question has nothing further to check, since a boolean cannot be malformed). Invalid or unparseable candidates are dropped, and a batch with some bad candidates yields the valid subset rather than failing the whole request. Question and option text is length capped and stored as plain text, never rendered as raw HTML (security.md section 1).
- **AC-7**: A teacher can attach source material for generation as pasted text or as an uploaded file (PDF or docx). A file is refused before parsing if its size exceeds the cap (enforced at the ASP.NET request pipeline level, not only in the handler, so the framework never buffers a huge body first) or if its real content, checked by magic bytes (`%PDF-`, `PK\x03\x04`) and not the client supplied content type or extension, is neither PDF nor docx. It is then parsed to text by a bounded extractor: extraction stops at the text cap and under a time limit, so a decompression bomb (a small docx or PDF that expands to gigabytes) cannot exhaust memory or hang the parser. The extracted text is used only for that generation and the file is never stored. An unsupported type, an oversized file, or a parse failure is refused with a clear message. The extracted text is subject to the same data minimization as the topic (AC-5).
- **AC-8**: Generated candidates are persisted server side as a pending draft batch tied to the quiz and the teacher, so a refresh does not lose them. Regenerating replaces that quiz's pending batch, so at most one pending batch exists per quiz. The teacher reviews the batch and either accepts chosen candidates, which promotes them onto the quiz through the same add question validation as a manual add, or discards the batch, which clears it. Accepting or discarding leaves no pending drafts behind.
- **AC-9**: Once a quiz has at least one attempt, its question set is locked: adding, editing, or deleting a question, and accepting drafts onto it, are refused with 409, so a student is never graded against a question set that changed under them. The publish settings (window and attempt limit) may still change, and the quiz may still be unpublished.
- **AC-10**: The whole authoring flow is driveable by a real teacher in the SPA without the API or the seeder: create a quiz in a class, build its questions by hand or by generation and review, set the duration, window, and attempts, and publish. Every authoring, generation, and draft read and write is owner scoped by the JWT `UserId`; a non owner acting on a quiz gets 404, so a quiz's existence never leaks (consistent with the take path and spec 0008).
- **AC-11**: A student sees and can start a quiz the moment their teacher publishes it (within the window, enrolment permitting), and the questions they take are exactly the ones the teacher accepted or wrote. This is the loop closing on real, not seeded, content: create class, join, author, publish, take, results, feedback.

## Decision

**Chosen option**: Option 1: build all five parts as one child of the core loop umbrella: publish, manual authoring, real Claude generation on the existing community SDK, server saved review drafts, and file backed source material, plus the full authoring UI.

Publish is the writer of the availability window and attempt limit, which no endpoint sets today, and the take path already reads them, so publish needs no take side change (unlike spec 0008's archive gap). Generation replaces the local stub with a real `claude-opus-4-8` strategy behind the feedback path's flag plus key gate, validated by hand because the community SDK gives no schema guarantee, with per candidate validation so a partly bad batch still yields its good questions, and empty editable templates as the deterministic fallback. Source files are parsed to text and thrown away, never stored, which keeps the upload surface to validation and parsing rather than storage and retention. Drafts are a small persisted entity, one pending batch per quiz, cleared on accept or discard, so a review survives a refresh without a background sweeper. A quiz's questions lock the moment it has an attempt, so easier AI driven editing cannot corrupt grading.

**Implementation skills**: `quiztin-design` (the project's design system skill, `.claude/skills/quiztin-design/`) for the authoring and generation screens; `claude-api` (Anthropic's API reference skill) for the generation call's model id, parameters, and validation posture.

## Feature design

**Data model sketch** (Assessment module, `quiz` schema):

- **Quiz** (existing entity, no new columns): publish writes the columns that already exist. `IsPublished` (bool, default false), `AvailableFrom` (DateTime?, null means no lower bound), `AvailableTo` (DateTime?, null means no upper bound), `MaxAttempts` (int, default 1). `CreatedByTeacherId` (Guid, the owner, already present). These have never been written by any endpoint; publish is their first writer.
- **GeneratedQuestionDraft** (new entity, the review draft; **one row per quiz**, holding the whole pending batch)
  - `Id` Guid, primary key
  - `QuizId` Guid, **real FK to `Quiz.Id`** within the module, with a **UNIQUE index**, so at most one pending batch per quiz is a database invariant rather than a delete then insert race
  - `Candidates` jsonb: the list of candidate questions, each `{ questionType, prompt, points, options?, correctOptionIndex?, correctAnswerBool?, correctAnswerText? }`, stored the same jsonb way `MultipleChoiceQuestion.Options` and the attempt's draft answers already are in this context
  - `CreatedAt` DateTime
  - The batch is owned by whoever owns the quiz (derivable through `QuizId`), so no separate teacher id is stored; every draft endpoint is already behind the quiz ownership 404 gate. Regenerating **upserts** this one row; a draft is always pending, so presence is the only state (accept promotes the chosen candidates and deletes the row, discard deletes the row).

**State transitions**:
- Quiz publish state: `unpublished` (IsPublished false, the create default) publishes to `published`, and unpublishes back. Orthogonal to that, a quiz is `editable` until its first attempt, then `locked` for structural question edits for the rest of its life (publish settings still change). Locked is derived from attempt existence, not stored.
- Draft batch: does not exist, then `pending` on generate (the one row present), then gone on accept (chosen candidates promoted to Questions, the row deleted) or discard (row deleted). A new generate upserts the one row; the unique index on `QuizId` makes at most one batch a database invariant, so a double submit cannot leave two.

**API surface** (Assessment module; `owner` = the caller's JWT `UserId` equals the quiz's `CreatedByTeacherId`, else 404):

| Endpoint | Method | Key inputs | Key outputs | Auth | Key errors |
|---|---|---|---|---|---|
| `/api/classrooms/{classroomId}/quizzes` | POST | `title`, `durationMinutes` | quiz summary | classroom owner (Teacher) | 400 invalid, 404 not your class |
| `/api/classrooms/{classroomId}/quizzes` | GET | `classroomId` | `[{id, title, isPublished, questionCount, attemptCount}]` | classroom owner | 404 not your class |
| `/api/quizzes/{quizId}` | GET | `quizId` | full quiz for editing (questions, settings, publish state, locked flag) | owner | 404 not yours |
| `/api/quizzes/{quizId}/questions` | POST | one question (type, prompt, points, answer) | quiz | owner | 400 invalid, 404, 409 locked |
| `/api/quizzes/{quizId}/questions/{questionId}` | PATCH | one question | quiz | owner | 400, 404, 409 locked |
| `/api/quizzes/{quizId}/questions/{questionId}` | DELETE | ids | 204 | owner | 404, 409 locked |
| `/api/quizzes/{quizId}/generate` | POST (multipart/form-data) | `topic`, `difficulty`, `count`, `sourceText?` as form fields; optional `file` part | the pending draft batch | owner | 400 invalid, 404, 409 locked, 413 file too large, 415 unsupported type |
| `/api/quizzes/{quizId}/drafts` | GET | `quizId` | the pending draft batch, or empty | owner | 404 |
| `/api/quizzes/{quizId}/drafts/accept` | POST | `draftIds` | quiz | owner | 404, 409 locked |
| `/api/quizzes/{quizId}/drafts/discard` | POST | `quizId` | 204 | owner | 404 |
| `/api/quizzes/{quizId}/publish` | POST | `availableFrom?`, `availableTo?`, `maxAttempts` | quiz | owner | 400 no questions / bad window, 404 |
| `/api/quizzes/{quizId}/unpublish` | POST | `quizId` | quiz | owner | 404 |

Note two existing seams: `POST /api/quizzes/{quizId}/generate` and `GET /api/quizzes/{quizId}` already exist. Generate is a stub today and becomes real here, and it is **always** `multipart/form-data` (the file is an optional part), so there is one request shape, not a JSON or multipart branch, which ASP.NET model binding does not cleanly support anyway. `GetQuizAsync` is deliberately unscoped today; the authoring detail is a **new owner scoped read**, not that method. Build task 2 checks whether anything still calls the unscoped `GetQuizAsync` and removes it if not, so the codebase does not keep an unscoped quiz read as reachable dead code.

**Key invariants**:
- Publish writes `IsPublished`, `AvailableFrom`, `AvailableTo`, `MaxAttempts` and nothing else writes them; the take path reads them, so the two stay consistent by there being one writer. Publishing a quiz whose classroom is archived is allowed but inert: the take path blocks an archived classroom's quizzes independently of `IsPublished` (spec 0008), so it becomes takeable only once the class is unarchived.
- A quiz cannot be published with zero questions.
- Question validation lives in one place: `QuestionFactory` gains `TryCreate...` methods that report success or failure instead of throwing, and both the manual add path and the generation candidate validation go through them, so the two paths cannot drift on what a valid question is.
- Once a quiz has any attempt, its `Questions` collection is immutable (add, edit, delete, and draft accept all refuse with 409). Attempt existence is the lock.
- At most one pending draft batch exists per quiz; a new generate deletes the quiz's prior drafts before inserting.
- The model receives only the topic, difficulty, count, and attached source material; nothing student, classroom, or teacher identifying, and no other quiz content (security.md section 2). Prompts and responses are never logged (section 6).
- Model output is validated per candidate against `QuestionFactory` rules before persistence; an invalid candidate is dropped, never allowed to fail the whole batch, and never constructed (which would throw). Prompt and option text is length capped; stored and rendered as plain text.
- An uploaded file is never persisted; it is parsed to text, capped, used for the one generation, and discarded. Type is allowlisted (PDF, docx) and size is capped before parsing.

**Security model**:
- **Owner only**, every endpoint: the caller's `UserId` must own the quiz (its classroom's teacher), else 404, so a quiz's existence never leaks. Create and list are gated on the classroom being the caller's; the rest on the quiz being theirs.
- **The model call** (security.md sections 1, 2, 3, 6): reached only through the Assessment module and the community `Anthropic.SDK`; the key lives in configuration or user secrets, never in the frontend; send only topic, difficulty, count, and source text; validate and cap the untrusted output; never log the prompt, the response, or the key.
- **File upload** is the new surface, and its bar is a mechanism, not an intent (the cross check found the first draft too thin here): cap the request size at the ASP.NET pipeline level (Kestrel `MaxRequestBodySize` and the multipart form limit), not only in the handler, so the framework never buffers a huge body before the check runs; verify the real format by magic bytes (`%PDF-`, `PK\x03\x04`), not the client supplied content type or extension, and treat a mismatch or a parse failure as unsupported; parse with managed libraries configured to not resolve external entities or DTDs (docx is a zip of XML, so XXE is the risk on that side); bound the extractor by both the text cap and a time limit so a decompression bomb (a small docx or PDF that expands to gigabytes) cannot exhaust memory or hang; store nothing. The generate and upload endpoints deserve a per user rate limit so neither the model spend nor the parser can be driven in a loop; the app has no rate limiting yet, so this is a Follow up, not a blocker.
- No student PII is ever involved in authoring; the data protected here is the teacher's own quiz content plus whatever they choose to attach.

**Configuration required**:
- `Generation:AiEnabled` (bool): the flag that turns real generation on, parallel to `Feedback:AiEnabled`. Off, or no key, means the empty template fallback.
- `Anthropic:ApiKey` (string): the shared key the feedback path already reads; no new secret. Never sent to the frontend, never logged.
- `Generation:Model` (string, default `claude-opus-4-8`): the generation model, separate from the feedback model so the higher stakes call can differ.
- `Generation:MaxCount` (int) and `Generation:MaxSourceChars` (int): the count cap and the source text cap, so a request cannot blow up the token spend.

**Critical test scenarios** (each maps to an acceptance criterion):
- Happy path, loop closes on real content: a teacher creates a quiz, generates and accepts questions, sets a window and attempts, and publishes; an enrolled student then sees it in their available list and starts it, with no seeded quiz involved, verifies **AC-1**, **AC-4**, **AC-8**, **AC-10**, **AC-11**.
- Publish gate: publishing a quiz with no questions is refused (400), and a bad window (from not before to) is refused (400); a valid publish flips it live and unpublish removes it from the student's available list, verifies **AC-1**, **AC-2**.
- Generation fallback and partial validity: with AI disabled, generate returns the requested number of empty templates; with a stubbed model returning a batch where one candidate is malformed, the valid candidates are persisted as drafts and the bad one is dropped rather than failing the request, verifies **AC-4**, **AC-6**.
- Upload safety: an oversized file and an unsupported type are each refused with a clear status; a valid PDF and docx parse to capped text and are not stored, verifies **AC-7**.
- Lock on attempt: after a student has an attempt, adding, editing, deleting a question, and accepting a draft onto the quiz are all refused (409), while changing the window still succeeds, verifies **AC-9**.
- Auth and ownership: a non owner generating, editing, publishing, or reading drafts on a quiz gets 404, verifies **AC-3**, **AC-10**.

## Build plan

Ordered as the umbrella's breadth first stance implies (foundation section 0), but landing the thin end to end thread (publish) first, because publish alone closes the loop that everything else improves. The data model migration for drafts lands with the generation work that needs it (task 3), not at the very front, because publish and manual editing need no schema change.

1. Publish and quiz settings (backend): a `POST /api/quizzes/{quizId}/publish` that validates ownership, at least one question, and a sane window, then writes the window, attempts, and `IsPublished`; an `unpublish`; owner scoped. No migration (the columns exist). Satisfies **AC-1**, **AC-2**.
2. Manual authoring (backend): owner scoped quiz detail and per class quiz list reads (a new scoped read, not the existing unscoped `GetQuizAsync`, which is removed if nothing else calls it); `QuestionFactory.TryCreate...` methods (success or failure, no throw) as the one validation path both manual and generated questions use; edit and delete question endpoints; a `HasAnyAttemptAsync(quizId)` on the attempt repository (a quiz wide check, which the student scoped methods do not offer today) backing the lock on attempt rule, applied to add, edit, and delete here and to draft accept in task 3. Satisfies **AC-3**, **AC-9**, **AC-10**.
3. Drafts and real generation (backend): the one row per quiz `GeneratedQuestionDraft` entity (unique `QuizId`, jsonb candidates) and migration; a real `AiQuestionGenerationStrategy` on the community `Anthropic.SDK` with `claude-opus-4-8`, behind a `Generation:AiEnabled` plus `Anthropic:ApiKey` gate mirroring the feedback wiring. The model's JSON is parsed element by element (tolerant, not the feedback path's one shot deserialize that fails whole on one bad element), each candidate validated through the `TryCreate` methods, invalid or unparseable ones dropped, and the empty template fallback used when AI is off or the call fails; generate upserts the one pending batch; the drafts read, accept (refused 409 if the quiz has attempts), and discard endpoints. Satisfies **AC-4**, **AC-5**, **AC-6**, **AC-8**, **AC-9**.
4. Source material (backend): pasted text as a form field on the generate request, and a file upload part that caps the request at the ASP.NET pipeline level, verifies the format by magic bytes (not content type or extension), parses with managed libraries configured against external entity resolution, bounds the extractor by both a text cap and a time limit so a decompression bomb cannot exhaust memory or hang, uses the text for the one generation, and stores nothing. Satisfies **AC-7**.
5. Authoring UI (frontend): a teacher quiz list per class, a create quiz action, and a quiz editor that adds, edits, and deletes questions by hand, sets duration, window, and attempts, and publishes or unpublishes, with the locked state reflected. Satisfies **AC-3**, **AC-9**, **AC-10**, **AC-11**.
6. Generation UI (frontend): a generate panel (topic, difficulty, count, paste or upload) on the editor, and a review step that shows the pending batch and lets the teacher edit, accept chosen candidates, or discard. Satisfies **AC-4**, **AC-7**, **AC-8**.
7. Tests and end to end verify: backend tests for the publish gate, generation fallback and partial validity and data minimization, upload safety, the lock on attempt, and owner scoping; frontend tests for the editor, the generation and review flow, and accessibility; then a full walk proving a teacher can author and publish a quiz that a real enrolled student then takes. Satisfies **AC-1** through **AC-11**.

## Consequences

**Positive**:
- The loop finally closes on real content: a teacher authors and publishes, a student takes, with nothing seeded. This is the last structural gap in the create, take, results, feedback loop.
- The AI differentiator becomes real on the authoring side, matching the feedback side (spec 0005), and reuses that path's gate, key, and fallback discipline rather than inventing a second one.
- Publish being the single writer of the window and attempts keeps those fields consistent with the take path by construction.
- Locking a quiz's questions once attempted removes a real grading integrity risk that easier AI editing would otherwise amplify.

**Negative / tradeoffs**:
- This is the largest child by far: five sub areas, about a dozen endpoints, two new security surfaces (the model call and file parsing), a new entity, and a large authoring UI. It will build over more steps than any prior slice and was flagged as spanning several decisions; it is taken as one at the engineer's direction.
- Staying on the community SDK means no schema guaranteed output, so a real generation depends on hand validation holding; the per candidate drop and the deterministic fallback are what keep a bad batch from becoming a bad quiz, and they must be tested against realistic model output, not just the stub.
- File upload adds a parser dependency and an attack surface (malicious PDF, docx XXE, oversized input) that a topic only generator would not have; it is bounded by allowlisting, size and text caps, not storing the file, and a rate limit follow up.
- Not snapshotting questions per attempt means the lock is permanent once a quiz is attempted; a teacher who wants to change a live, attempted quiz must make a new one. This is the safe default; per attempt snapshotting stays a later option.

**Neutral**:
- The generate endpoint gains a multipart shape for the file path alongside its JSON shape; the frontend sends one or the other.
- New configuration (`Generation:AiEnabled`, `Generation:Model`, the caps) sits beside the existing feedback configuration; the API key is shared, so no new secret enters git or the environment.
- Two new backend dependencies (a PDF text extractor and a docx reader) land; `library-docs.md` records them.

## Follow-up

- [ ] Rate limit `POST /api/quizzes/{quizId}/generate` and the file upload path per user (ASP.NET Core rate limiting), so neither the model spend nor the parser can be driven in a loop. Shares the prerequisite with spec 0008's rate limit follow up: the app has no rate limiting middleware yet.
- [ ] Sweep abandoned pending draft batches on a schedule (a teacher who generates and navigates away leaves one batch per quiz). Deferred because foundation section 8 defers background sweepers; the one batch per quiz cap bounds the leak until then.
- [ ] Consider per attempt question snapshotting, which would let a teacher edit a live quiz without the permanent lock and would also fix the pre existing risk that feedback reads questions fresh at grading time (flagged in spec 0008's cross check). Larger than this slice.
- [ ] The lock on attempt check is a read then write with no transaction, so a teacher's edit and a student's very first `StartQuizAsync` can in principle interleave (the same narrow window as the fresh read risk above; Postgres Read Committed does not close it alone). A row lock or the snapshot above would. Bounded and rare, deferred with the snapshot item.
- [ ] Now that publish makes `MaxAttempts` a real, teacher set, non default value for the first time, revisit the available quizzes state: a student who abandoned through all their attempts without ever finishing shows `NotStarted` and then hits a raw error on start (pre existing, spec 0006). Give that case its own state or message.
- [ ] Reconcile the context system when this lands: cite 0009 from `foundation.md` (the core loop children and UC6), flip UC6 from "mostly built" to "built" and record the publish step in `build-graph.md`, and record the new PDF and docx parsing libraries in `library-docs.md`.
- [ ] Confirm whether short answer grading should stay exact match now that AI generation will raise short answer volume; `ShortAnswerQuestion.IsCorrect` is exact match only today and already flags fuzzier or AI grading as a later enhancement.

## Rationale

Reasoning, the options weighed, and the premise note live in [rationale.md](rationale.md).
