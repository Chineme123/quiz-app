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
