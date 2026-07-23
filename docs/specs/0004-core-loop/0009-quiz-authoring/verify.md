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
- **AC-9** lock on attempt covers generate and accept: the attempt-then-409 step. Task 4 (source material) and tasks 5 to 6 (UI) append their AC-7/AC-10/AC-11 steps.
