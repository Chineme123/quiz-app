# Verify: Teacher classroom results · spec 0010

_Steps derived from spec 0010's thirteen acceptance criteria. **Built.** The backend path was driven live end-to-end with minted JWTs (see "Live drive result" below); the signed-in UI walk is what remains for `/check verify`. `/test` locks the durable ones._

## Automated coverage (green as of the build)
- **Backend integration tests** — `tests/Quiztin.Modules.Assessment.Tests/ClassroomResultsTests.cs` (7 tests, real Postgres via Testcontainers): the dedup to one attempt per student and the "latest submitted" rule (AC-7), the class average and completion count (AC-2), per-question fraction over one-per-student (AC-4), the submitted-score-stands-over-a-newer-open-attempt rule and In progress / Not taken (AC-8), percentage-normalized overall standing (AC-5), archived served (AC-9), owner-scoped 404 across all three aggregate endpoints (AC-1), the drill-down's latest-submitted selection and owner scoping (AC-6), and the display-name / email-fallback resolution (AC-13). Run: `dotnet test --filter FullyQualifiedName~ClassroomResultsTests`.
- **Frontend tests** — `frontend/src/features/classroom-results/*.test.tsx` (5 files: the summary, per-quiz, roll-up, drill-down pages, plus a schema test) and the shared `features/results/ResultsPage.test.tsx` (guards the extracted `AttemptAnswerReview`). Each page test includes an axe pass (AC-12). Run: `npx vitest run src/features/classroom-results src/features/results`.

## Setup
- [ ] Postgres up and the API in Development. **The seeder now creates the results demo data automatically** (spec 0010): the teacher (`11111111-…1`), the classroom (`33333333-…3`, code `SEED23`), the published 3-question quiz (`44444444-…4`, "Networking Basics (Dev)", 3 points), and three students — **Sam Carter** (`22222222-…2`, submitted 2/3), **Alex Rivera** (`22222222-…5`, submitted 3/3), and **Jordan Lee** (`22222222-…6`, enrolled, not taken). So the summary reads completion 2 of 3, average 2.5/3 (83.3%).
- [ ] Mint JWTs directly (no passwords, the house convention): the seed teacher, a non-owner teacher (any other Guid), and a student token, to prove scoping. HS256 over `JwtSettings__Secret`, claim `nameid` = the user Guid, `iss`/`aud` = `quiztin`.
- [ ] The retried-attempt and in-progress edge cases (AC-7 dedup, AC-8) are not in the seed (they are transient); they are locked by `ClassroomResultsTests`, or create them by hand for the UI.

## API (owner scoped; bearer token per step) — confirmed on the live drive
- [x] `GET /api/classrooms/{classroomId}/results` as the **owning teacher** → 200, a row per published or attempted quiz with title, completion count, and class average. → AC-2
- [x] The same call as a **non owner** (and a made-up `classroomId`) → 404, indistinguishable. A student token → 404 too (the calm not-found, not a 403). → AC-1
- [x] `GET /api/quizzes/{quizId}/results` as the owner → per-question fraction correct, plus a paginated per-student list (latest submitted score, or "Not taken" / "In progress"). Confirmed: fractions 100% / 100% / 50%, and the three student rows by name. → AC-3, AC-4
- [ ] Give one student two submitted attempts with different scores → the **latest** shows everywhere, and the earlier does not skew the average or the per-question fraction (locked by `ClassroomResultsTests`). → AC-7, AC-4
- [ ] Give a student a submitted score **and** a newer still-open attempt → the submitted score shows, not "In progress" (locked by `ClassroomResultsTests`). → AC-8
- [x] Every student row shows a **display name** (Sam Carter, Alex Rivera, Jordan Lee), resolved from Identity in process; the email fallback covers a student with no profile name. Never a bare Guid. → AC-13
- [x] `GET /api/classrooms/{classroomId}/results/students` → paginated (default 20, max 50), each student a score per quiz plus an overall standing (Sam 66.7%, Alex 100%, Jordan null). → AC-5, AC-10
- [x] `GET /api/quizzes/{quizId}/results/students/{studentId}` as the owner → the student's latest submitted attempt with its per-question detail. A non-owner, and a student with no submitted attempt (Jordan), → 404. → AC-6
- [ ] Archive the classroom, then repeat the summary as the owner → still 200 (locked by `ClassroomResultsTests`). → AC-9
- [x] No migration was added and no results table exists (`git status`, and the read layer queries the existing attempt tables). → AC-11

## UI / manual (signed in as the seed teacher — the part left for /check verify)
- [ ] From the class page, follow "View results" → the per-quiz summary renders (completion and average per quiz), with a way into each quiz and into the per-student roll-up. → AC-12, AC-2
- [ ] Open a quiz's results → per-student scores and per-question difficulty; misses are framed calmly ("to review" / "worth reviewing together"), per `ui-rules.md`. → AC-3, AC-4
- [ ] Open the per-student roll-up → each student's arc across the quizzes plus an overall standing. → AC-5
- [ ] Drill from a student's score into their attempt → the per-question detail, in the teacher voice. → AC-6
- [ ] A classroom with no quizzes, and a quiz with no attempts, each show a calm empty state, not a blank or an error. → AC-2, AC-3
- [ ] Signed out, the results routes redirect to sign in; a signed-in student who reaches a results URL gets the calm not-found (not a 403 screen). → AC-1, AC-12

## Live drive result (this build)
Booted the host on a fresh `quiztin_v10` DB (migrate + seed), minted a teacher JWT, and drove all four endpoints: the summary (completion 2, average 2.5/3 = 83.3%), the per-quiz view (fractions 100/100/50, three named rows, one Not taken), the roll-up (Sam 66.7%, Alex 100%, Jordan null), and Sam's drill-down (score 2, three answers, TCP wrong). A non-owner got 404 on every endpoint; Jordan's drill-down (no submitted attempt) was 404. Every payload parsed clean against the frontend zod schemas (camelCase, status enum strings, nullables). The cross-module name lookup resolved live. **One bug found and fixed:** the seeder queried the quiz before its own save, so attempts seeded as zero; reordered to seed attempts after the precondition save.

## Acceptance-criteria coverage
- **AC-1** owner only, non owner and not found both 404 · **AC-2** classroom summary · **AC-3** per quiz latest submitted / Not taken / In progress · **AC-4** per question fraction correct · **AC-5** roll-up with overall standing · **AC-6** drill down · **AC-7** latest submitted counts · **AC-8** in progress / non takers excluded · **AC-9** archived still served · **AC-10** paginated 20/50 · **AC-11** read only, no table · **AC-12** screens behind sign in and ownership · **AC-13** display names (email fallback), resolved from Identity in process.
