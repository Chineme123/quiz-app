# Verify: Take quiz screen · spec 0006 · updated 2026-07-16
_Steps derived from spec 0006 acceptance criteria. `/check verify` runs these; `/test` locks the durable ones._
_Covers the whole child: backend (tasks 1 to 8) and the SPA (tasks 9 to 14)._

## Commands
- [ ] `dotnet test QuizApp.sln` → 61 pass (Auth 27, User 9, Quiz 25). → AC-5, AC-7, AC-12, AC-14, AC-15, AC-16
- [ ] `docker compose exec postgres psql -U postgres -d quizdb -c '\d "QuizAttempts"'` → `DraftAnswers` jsonb default `'{}'::jsonb`, `ExpiresAt` timestamptz not null, `AbandonReason` text nullable. → AC-3, AC-6

## API (mint a student JWT for the seeded student, call through the gateway)
- [ ] `GET /api/quizzes/available` → only the enrolled, published, in-window quiz; carries `state` and the open `attemptId`; `total`, `page`, `pageSize` present. A quiz in a classroom the student is not enrolled in never appears. → AC-1, AC-2
- [ ] `POST /api/quizzes/{quizId}/start` → `{attemptId}`; the row's `ExpiresAt` equals start + the quiz's DurationMinutes. → AC-3
- [ ] `GET /api/attempts/{attemptId}/questions` → questions carry prompt, points, options and **no correct answer**; response also carries `expiresAt`, `serverNow`, and `draftAnswers`. → AC-4, AC-9, AC-10
- [ ] `PUT /api/attempts/{attemptId}/answers` with the whole answer map → `204`. Repeat it → `204` again (safe to retry). → AC-6
- [ ] Same `PUT` as a *different* student → `404`, never revealing the attempt exists. → AC-5
- [ ] `GET /api/attempts/{attemptId}/questions` as a *different* student → `404`. → AC-5
- [ ] `POST /api/attempts/{attemptId}/submit` with a body of only `{commandId}` → graded score computed from the saved drafts (no answers in the body). Replay the same `commandId` → the same result, no re-grade. → AC-11
- [ ] Same submit as a *different* student → `404`; the owner's attempt is still `InProgress` and unscored. → AC-5
- [ ] Start the same quiz again → the previous attempt becomes `Abandoned` with reason `Superseded`. Repeat until the limit: the seed quiz is `MaxAttempts = 5`, so the 6th start is refused with "Maximum attempts (5) exceeded" (each restart consumed one). → AC-15

## UI / manual (sign in as the seeded student)
- [ ] `npx vitest run src/features/take` → 12 pass, including two axe checks. → AC-17, AC-19
- [ ] Visit `/quizzes` signed out → redirected to sign-in (the route is behind RequireAuth). → AC-1
- [ ] Visit `/quizzes` signed in → the seeded quiz shows **Start**; a quiz you are not enrolled in never appears. → AC-1, AC-2
- [ ] Click **Start** → lands on `/attempts/{id}/take`, question 1 of N, countdown running in the sticky header. → AC-3, AC-10
- [ ] Answer a question → the header shows "Saving…" then "Saved". → AC-6, AC-8
- [ ] **Reload the page mid-attempt** → your answers come back and the countdown continues against the same deadline (it does not restart). → AC-9
- [ ] Use the navigator grid: jump between questions, and tab to it with the keyboard alone → each button announces "Question N, answered / not answered yet". → AC-17, AC-19
- [ ] Click **Submit quiz** with a blank question → the dialog names how many are unanswered and still lets you submit. → AC-13
- [ ] Confirm → lands on `/results/{attemptId}` with the score, then the feedback. → AC-18
- [ ] Return to `/quizzes` → that quiz now shows **View result**; an open attempt shows **Resume**. → AC-2
- [ ] Open `/attempts/{someone-elses-id}/take` → a calm "we couldn't find that quiz", never a hint it exists. → AC-5

## Acceptance-criteria coverage
- AC-1 available list, enrolment scoped + paginated · AC-2 per-quiz action + open attemptId · AC-3 ExpiresAt pinned at start · AC-4 questions via the attempt, no correct answers · AC-5 scoped 404 on every attempt endpoint · AC-6 whole-set draft save · AC-7 draft rejected past the deadline · AC-8 save-state indicator · AC-9 resume restores saved answers · AC-10 countdown on the server clock, auto-submit at zero · AC-11 submit grades saved drafts, idempotent · AC-12 late submit grades rather than abandons · AC-13 unanswered confirm warns without blocking · AC-14 window gates start only · AC-15 supersede consumes the limit · AC-16 superseded submit is a handled 409 · AC-17 wizard + navigator + all three question types · AC-18 redirect to results · AC-19 keyboard and screen reader.
