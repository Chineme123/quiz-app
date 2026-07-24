# Verify: quiz authoring · spec 0009 · updated 2026-07-23

_Steps derived from spec 0009 acceptance criteria. `/check verify` runs these; `/test` locks the durable ones. This file grows per task; the steps below are **task 2 (manual authoring)**, covering AC-3, AC-9, AC-10. Tasks 3 to 7 (generation, drafts, upload, UI) append their AC steps here as they land._

## API / manual (drive the live app, authenticated as the owning teacher unless noted)
- [ ] `POST /api/classrooms/{ownedId}/quizzes` `{title, durationMinutes}` → 201; the same on a class you do not own → 404; a blank title or a zero duration → 400 → AC-3, AC-10
- [ ] `POST /api/quizzes/{quizId}/questions` once each for MultipleChoice, TrueFalse, ShortAnswer → 200 each, and a follow up `GET` shows all three → AC-3
- [ ] `POST .../questions` a MultipleChoice with one option → 400 `{error}`; an unknown type "Essay" → 400 → AC-3
- [ ] `PATCH /api/quizzes/{quizId}/questions/{questionId}` with a new prompt, points, and answer → 200, and `GET` reflects the change; a `PATCH` whose type differs from the stored question → 400 → AC-3
- [ ] `DELETE /api/quizzes/{quizId}/questions/{questionId}` → 204, and `GET` shows it gone while the other questions remain → AC-3
- [ ] As a non owner: `POST` / `PATCH` / `DELETE` a question, `GET /api/quizzes/{quizId}`, and `GET /api/classrooms/{classroomId}/quizzes` → 404 every time (existence never leaks) → AC-3, AC-10
- [ ] `GET /api/quizzes/{quizId}` as the owner → 200 carrying questions, settings, publish state, and `isLocked: false` → AC-10
- [ ] `GET /api/classrooms/{ownedId}/quizzes` → 200 array of `{id, title, isPublished, questionCount, attemptCount}` → AC-10
- [ ] An enrolled student starts an attempt on the quiz, then the teacher `POST` / `PATCH` / `DELETE` a question → 409 each, and the detail read now shows `isLocked: true` → AC-9
- [ ] With the quiz locked (an attempt exists), `publish` and `unpublish` still return 200 → AC-9

## Commands
- [ ] `dotnet test tests/Quiztin.Modules.Assessment.Tests` → 69 pass (includes `QuestionFactoryTests` and `ManualAuthoringTests`) → AC-3, AC-9, AC-10
- [ ] `dotnet build Quiztin.sln` → 0 errors

## Acceptance-criteria coverage (task 2)
- **AC-3** (create + add/edit/delete each type, one validation rule set, non owner 404): the create, per type add, invalid add, edit, delete, and non owner steps.
- **AC-9** (question set locks once attempted; publish settings still change): the attempt then 409 step, and the publish while locked step.
- **AC-10** (authoring is owner scoped by the JWT UserId, non owner 404): the non owner step, the owner scoped detail read, and the class list step. The SPA half of AC-10 is task 5.

## API / manual (task 3: generation + review drafts)
- [ ] `POST /api/quizzes/{quizId}/generate` `{topic, difficulty, count}` with AI off → 200 with `count` empty template candidates; `GET /api/quizzes/{quizId}/drafts` reads the same batch back → AC-4, AC-8
- [ ] With `Generation:AiEnabled=true` and a key set, the same call returns candidates from `claude-opus-4-8`; a malformed element in the model's array is dropped, not fatal (the valid ones still land) → AC-4, AC-6
- [ ] Generate again → the one batch is replaced (exactly one draft row per quiz), and `GET .../drafts` shows only the new candidates → AC-8
- [ ] `POST /api/quizzes/{quizId}/drafts/accept` `{draftIds}` for chosen candidates → 200, they become questions on the quiz, and `GET .../drafts` is empty afterward → AC-8
- [ ] Accept a draftId whose candidate is an unfilled template → 400, no question added, batch still present → AC-8
- [ ] `POST /api/quizzes/{quizId}/drafts/discard` → 204, batch gone; discarding again → 204 → AC-8
- [ ] An enrolled student starts an attempt, then the teacher generates and accepts → 409 each → AC-9
- [ ] Generate with a blank topic → 400 → AC-4
- [ ] As a non owner: generate, `GET .../drafts`, accept, discard → 404 each → AC-10
- [ ] The student sees and can start a quiz built entirely from accepted candidates, no seed → AC-11

## Commands (task 3)
- [ ] `dotnet ef database update --context QuizDbContext --connection <throwaway postgres>` → `quiz.GeneratedQuestionDrafts` exists with a unique `QuizId` index and a FK to `quiz.Quizzes` → AC-8
- [ ] `dotnet test tests/Quiztin.Modules.Assessment.Tests` → 85 pass (includes GeneratedCandidateParserTests and QuizGenerationTests) → AC-4, AC-6, AC-8, AC-9

## Acceptance-criteria coverage (task 3)
- **AC-4** generation fallback: the AI-off generate step and the malformed-element step.
- **AC-5** data minimization: only topic, difficulty, and count are sent; confirm no identifiers in a live run's request payload.
- **AC-6** untrusted output validated per candidate: the parser step (a mixed valid/invalid array keeps only the good ones).
- **AC-8** drafts persist, one batch per quiz, accept and discard clear it: the generate, regenerate, accept, and discard steps.
- **AC-9** lock on attempt covers generate and accept: the attempt-then-409 step.

## API / manual (task 4: source material)
- [ ] `POST /api/quizzes/{quizId}/generate` as `multipart/form-data` with fields topic, difficulty, count and `sourceText` (pasted) → 200, and the questions reflect the pasted material → AC-7
- [ ] The same call with a `file` part that is a real **docx** → 200, and the questions reflect the document's text → AC-7
- [ ] The same call with a real **PDF** → 200, and the questions reflect the PDF's text → AC-7
- [ ] A file whose bytes are neither PDF nor docx (a .txt renamed to .pdf, or a PNG) → **415**, whatever the declared content type or extension says → AC-7
- [ ] A file over the 5 MB cap → **413**, rejected at the request pipeline before the action runs (the body is never buffered) → AC-7
- [ ] A corrupt PDF or docx (right magic bytes, damaged content) → **400** with a clear message, no crash → AC-7
- [ ] A zip bomb docx (small file, huge `word/document.xml`) → **400**, and the server stays responsive: no memory spike, no hang → AC-7
- [ ] Nothing is stored: after a generate with a file, no file row or blob exists and the container holds no copy → AC-7
- [ ] Only task necessary content leaves: the outbound model request carries topic, difficulty, count, and the source text, and no identifiers → AC-5

## Commands (task 4)
- [ ] `dotnet test tests/Quiztin.Modules.Assessment.Tests` → 94 pass (includes SourceMaterialExtractorTests) → AC-7

## Acceptance-criteria coverage (task 4)
- **AC-7** (pasted or uploaded source, magic-byte type check, pipeline size cap, bounded extraction, nothing stored): the pasted, docx, PDF, wrong type, oversized, corrupt, and bomb steps.
- **AC-5** data minimization now also covers the extracted source text: the outbound payload step.

## In the browser (task 5: authoring UI)
Signed in as a teacher who owns the class.
- [ ] Open a class, follow **Manage quizzes** → the quizzes for that class, with each one's state and counts → AC-10
- [ ] Create a quiz (title plus minutes) → it appears in the list and the editor opens on it → AC-10
- [ ] Add one of each type (multiple choice, true or false, short answer) → each appears with its type, points, and **correct answer** → AC-3
- [ ] Edit a question → the change sticks, and the type is shown read only (changing type is a remove then add) → AC-3
- [ ] Remove a question → it confirms first, then the question goes → AC-3
- [ ] Publish with an availability window and an attempt limit → a student in that class can see and start it → AC-10
- [ ] Unpublish → it confirms first, and a student can no longer start it → AC-10
- [ ] Put another teacher's quiz id in the `/quizzes/{id}/edit` URL → "We couldn't find that quiz", the same as an id that does not exist → AC-1
- [ ] After a student has an attempt, reopen the editor → the add, edit, and remove controls are **not shown** and a line explains why; the availability and attempt settings still work → AC-9
- [ ] Keyboard only: reach and operate create, add, edit, remove, and publish, with a visible focus ring throughout → AC-3

## In the browser (task 6: generation and review UI)
- [ ] Draft questions from a topic alone → candidates appear **for review** and the quiz itself is unchanged → AC-4, AC-8
- [ ] Draft again → the waiting batch is replaced, not added to (one batch per quiz) → AC-8
- [ ] Skip some candidates, add the rest → only the kept ones land on the quiz, and the batch is cleared → AC-8
- [ ] Clear a batch → it confirms first, and the quiz is untouched → AC-8
- [ ] Add a candidate, then edit it like any other question → it edits normally (editing happens after accepting, not before) → AC-3
- [ ] Paste source material → the candidates reflect it → AC-7
- [ ] Attach a real docx, then a real PDF → the candidates reflect the document → AC-7
- [ ] Attach a file over 5 MB → refused in the page with a sentence, and **no request is sent** → AC-7
- [ ] With `Generation:AiEnabled=false` → the requested number of empty editable templates, never an error → AC-4
- [ ] With the model returning nothing usable → a plain line saying so, and the quiz is unchanged → AC-4
- [ ] A student takes a quiz built **entirely** from accepted candidates, with no seed data → it plays and scores normally → AC-11
- [ ] Keyboard only: move through the review list, toggle a candidate between kept and skipped, and confirm the toggle announces its pressed state to a screen reader → AC-8

## Commands (tasks 5 and 6)
- [ ] `npm run test` in `frontend/` → 143 pass, 21 of them in `src/features/authoring` → AC-3, AC-4, AC-8, AC-10
- [ ] `npm run build` in `frontend/` → green, and `dist/index.prerender.html` matches the same file built from the previous commit (compare the two files, not an absolute byte count: the `vite build` step is not byte deterministic between runs) → spec 0003 landing safety

## Acceptance-criteria coverage (tasks 5 and 6)
- **AC-3** author by hand: the add, edit, and remove steps, plus editing an accepted candidate.
- **AC-4** generation with a deterministic fallback: the topic only, AI off, and nothing usable steps.
- **AC-7** source material: the paste, docx, PDF, and oversized file steps.
- **AC-8** one batch per quiz, reviewed before anything is kept: the draft, redraft, partial accept, and clear steps.
- **AC-9** lock on attempt reaches the UI: the reopened editor step.
- **AC-10** the surface is driveable end to end: the manage quizzes, create, publish, and unpublish steps.
- **AC-11** a fully generated quiz is takeable: the student step.
