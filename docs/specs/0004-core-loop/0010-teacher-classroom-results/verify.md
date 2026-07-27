# Verify: Teacher classroom results · spec 0010

_Steps derived from spec 0010's twelve acceptance criteria. Seeded at design time; `/check verify` runs these after the build, `/test` locks the durable ones. Not yet built._

## Setup
- [ ] Postgres up and the API in Development, so the seeder creates the teacher, the student, the classroom (`SEED23`), and the published quiz. Add a second student and a couple more submitted attempts (including a second attempt for one student, and one in progress attempt) so the aggregates have something to show.
- [ ] Mint JWTs directly (no passwords): the seed teacher (`11111111-...1`, role Teacher) and a non owner teacher, plus a student token, to prove scoping.

## API (owner scoped; bearer token per step)
- [ ] `GET /api/classrooms/{classroomId}/results` as the **owning teacher** → 200 with a row per published or attempted quiz, each carrying the title, the completion count (distinct enrolled students with a submitted attempt), and the class average over those submitted attempts. Never published drafts are absent. → AC-2
- [ ] The same call as a **non owner teacher** and as a **student** → 404 both times; a made up `classroomId` → 404 as well (not owned and not found are indistinguishable). → AC-1
- [ ] `GET /api/quizzes/{quizId}/results` as the owner → per question fraction correct, plus a paginated per student list where each row is the student's latest submitted score, or "Not taken", or "In progress". → AC-3, AC-4
- [ ] Give one student two submitted attempts with different scores → the results show the **latest** submitted one everywhere, **and the earlier attempt does not skew the class average or the per question fraction** (aggregates are one row per student, not per attempt). → AC-7, AC-4
- [ ] Give a student a submitted score **and** a newer still open attempt → the submitted score shows (not "In progress"). A student with only an in progress attempt shows "In progress"; a student with no attempt shows "Not taken"; both are excluded from the average. → AC-8
- [ ] Every student row shows a **display name** (or the email fallback for a student with no profile name), never a bare Guid; the name comes from the Identity module in process. → AC-13
- [ ] Confirm the "submitted" filter uses `SubmittedAt` being set, not a state name: an attempt that has moved on to `Graded`/`Reviewable` (which happens within seconds) still counts. → AC-2, AC-3
- [ ] `GET /api/classrooms/{classroomId}/results/students` → paginated (default 20, hard max 50), each student with a score per quiz and an overall standing (average over taken quizzes). → AC-5, AC-10
- [ ] `GET /api/quizzes/{quizId}/results/students/{studentId}` as the owner → that student's latest submitted attempt with its per question detail (the same breakdown the student sees). As a non owner → 404. → AC-6
- [ ] Archive the classroom, then repeat the summary call as the owner → still 200 with the results. → AC-9
- [ ] Confirm no migration was added for this feature and no results table exists; the figures come from the existing attempt tables. → AC-11

## UI / manual (signed in as the seed teacher)
- [ ] From the class page, reach the classroom results screen → the per quiz summary renders (completion and average per quiz), with a way into each quiz and into the per student roll up. → AC-12, AC-2
- [ ] Open a quiz's results → per student scores (latest submitted, or Not taken / In progress) and the per question difficulty; misses are framed calmly ("to review"), per `ui-rules.md`. → AC-3, AC-4
- [ ] Open the per student roll up → each student's arc across the quizzes plus an overall standing; who is thriving and who needs a hand is legible at a glance. → AC-5
- [ ] Drill from a student's score into their attempt → the per question detail. → AC-6
- [ ] A classroom with no quizzes, and a quiz with no attempts, each show a calm empty state, not a blank or an error. → AC-2, AC-3
- [ ] `npx vitest run src/features/classroom-results` → green, including an axe pass. → AC-12
- [ ] Signed out, the results routes redirect to sign in; a student who reaches a results URL is refused. → AC-1, AC-12

## Acceptance-criteria coverage
- **AC-1** owner only, non owner and not found both 404 · **AC-2** classroom summary (completion, average, published or attempted quizzes) · **AC-3** per quiz per student latest submitted / Not taken / In progress · **AC-4** per question fraction correct · **AC-5** per student roll up with overall standing · **AC-6** drill down to an attempt's per question detail · **AC-7** latest submitted counts · **AC-8** non takers and in progress excluded from averages · **AC-9** archived classrooms still served · **AC-10** lists paginated 20 default, 50 max · **AC-11** read only, no table, computed on read · **AC-12** the three screens behind sign in and teacher ownership · **AC-13** student rows show display names (email fallback), resolved from Identity in process.
