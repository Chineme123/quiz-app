# 0011. My results and progress (student)

**Date**: 2026-07-28
**Status**: In Progress

## Summary

A student opens one place and sees every quiz they have finished, grouped by class, with the score they got and how they are doing overall in each class. Each row links into the per question breakdown they already see right after taking a quiz. It is read only: it adds no tables and writes nothing, it just reads and totals the attempt data that already exists, scoped to the signed in student. This is the student side counterpart to the teacher's classroom results (spec 0010), and the last read surface of the core loop (the UC9 item foundation section 8 names as in scope).

## Requirements

**User stories**:
- As a student, I want all my quiz results in one place, so I can look back at how I did without hunting through each class.
- As a student, I want to see how I am doing overall in a class, so I know where I stand.
- As a student, I want to reopen the per question feedback for any quiz I finished, so I can revise what I got wrong.

**Acceptance criteria** (the contract, each IDed and independently checkable):
- **AC-1**: The screen shows only the signed in student's own results. The endpoint reads the student id from the token; there is no student id in the request, so no other student's results are reachable by construction. An unauthenticated call is refused (401).
- **AC-2**: Results are grouped by classroom. **Every classroom the student is currently enrolled in appears as a group** (archived ones included, AC-7), so a class never silently disappears; each group shows the classroom name, whether it is archived, the quizzes in that class the student has finished, and a standing (below). A group where the student has finished nothing shows a gentle per group "nothing finished yet" note, distinct from the page level empty state (AC-8), rather than being omitted.
- **AC-3**: A quiz appears only when the student has at least one submitted attempt on it (SubmittedAt set). A quiz the student can take but has not started does not appear here; it stays in the existing available quizzes list.
- **AC-4**: When the student has more than one submitted attempt on a quiz, the row shows the **latest submitted** attempt (the one with the greatest SubmittedAt), the score that counts. The older attempt is never shown and never counted in the standing.
- **AC-5**: Each quiz row shows the score (points out of the quiz total), the percent, and the date submitted, and links to that attempt's per question detail, the same results screen the student sees right after taking the quiz. No new detail screen is built.
- **AC-6**: Each classroom group shows a standing: the student's average percent over the quizzes they have finished in that class. Each quiz score is first turned into a percent of that quiz's total points, then averaged, so quizzes of different sizes compare fairly. A class where the student has finished nothing shows no standing (and the per group empty note of AC-2).
- **AC-7**: An archived classroom the student is enrolled in still shows its results (archiving preserves history, spec 0008).
- **AC-8**: When the student has finished nothing anywhere, the screen shows a calm, encouraging empty state that points to available quizzes, not an error or a blank page.
- **AC-9**: An attempt still in progress (started, not yet submitted) is not a result row. If the student has one, the screen may show a small resume hint that links back into the take flow, but it is never counted as a result.
- **AC-10**: The feature is read only. It adds no table and performs no write; every figure is computed on read from the existing attempt data, scoped to the student.
- **AC-11**: The student can reach "My results" from the profile menu and from the student dashboard.

## Decision

**Chosen option**: Option 1: aggregate on read, student scoped, reusing the spec 0010 read layer.

Serve the student's own results by querying and totalling the existing `QuizAttempt`, `QuizAnswer`, `Enrollment`, `Quiz`, and `Question` data live in the Assessment module, scoped to the authenticated student, with no new table and no change to the grading path. Reuse spec 0010's read repository, its latest submitted and percentage logic, and the existing per attempt results screen for the detail.

**Implementation skills**: `quiztin-design` (`Chineme123/quiz-app`, `.claude/skills/quiztin-design/`, the results screen tokens and the calm, non punitive voice for a results view)

## Feature design

**Data model sketch**: **No new entities and no migration.** The read totals existing entities, scoped to the student:
- `Enrollment` (`StudentId`, `ClassroomId`, `EnrolledAt`) gives the classes the student is currently in; `Classroom` (`Id`, `Name`, `ArchivedAt`) gives the name and archived flag; `Quiz` (`Id`, `ClassroomId`, `Title`) with its `Question` set (each `Points`) gives the title and total points; `QuizAttempt` (`StudentId`, `QuizId`, `SubmittedAt`, `TotalScore`) gives the finished attempts.
- **One shared read underpins the screen: "the student's latest submitted attempt per quiz, across the classes they are enrolled in".** An attempt counts as submitted when `SubmittedAt` is set (see invariants); the latest per quiz is the one that shows and the one the standing counts.
- Values are derived on read, never stored: per quiz latest score and percent (score over the quiz's total `Points`); per class standing (the average of those per quiz percents over the quizzes finished in that class).
- **No cross module name lookup.** Unlike spec 0010 (which resolves other students' names through `IUserDirectory`), this is the student's own results, so nothing crosses the module boundary and no name resolution is needed.
- New read only DTOs (no persistence): `MyResultsDto { Classrooms: MyResultsClassroomDto[] }`; `MyResultsClassroomDto { ClassroomId, ClassroomName, IsArchived, StandingPercent?, Quizzes: MyResultsQuizDto[] }`; `MyResultsQuizDto { QuizId, Title, TotalPoints, Score?, Percent?, AttemptId, SubmittedAt }`. The per attempt detail reuses the existing `AttemptResultDto`.
- Reuses `IResultsReadRepository.GetSubmittedAttemptsAsync(quizIds, [studentId])` (its `SubmittedAt != null` filter gives the submitted predicate for free). The app service is a new `MyResultsAppService` composing that read repository, `IClassroomRepository`, and `IQuizRepository` (the same repositories 0010's summary composes, minus `IUserDirectory`; note `IQuizRepository` reads per classroom today, so quizzes are gathered per enrolled class, an N+1 that is fine at this scale). **Two build cautions, each guarding an acceptance criterion:**
  1. Read the student's enrolled classrooms with a method that **keeps archived classes** (AC-7). Do **not** reuse `IClassroomRepository.GetEnrolledAsync`, which filters `ArchivedAt == null` for spec 0008's opposite need (the active list): driving the quiz allow list off that would silently drop a student's graded work in an archived class from their results. Add a distinct read, for example `GetEnrolledClassroomsForResultsAsync(studentId)`, that includes archived classes.
  2. The `latest submitted` reduction and the percentage / zero guard are `private static` helpers on `ClassroomResultsAppService` today. Extract them into a shared internal helper both services call, so the two results views cannot drift, rather than copying the logic into `MyResultsAppService`.

**State transitions**: none. The feature is read only.

**API surface** (the one new endpoint is scoped to the token principal; the detail endpoint already exists and is reused, not rebuilt):

| Endpoint | Method | Key inputs | Key outputs | Auth | Key errors |
|---|---|---|---|---|---|
| `/api/results/mine` | GET | none (student id from the token) | classrooms, each with name, archived flag, standing percent, and the finished quiz rows (title, score, percent, date, attempt id) | bearer, any authenticated user (their own results) | 401 unauthenticated |
| `/api/attempts/{attemptId}/result` | GET | attemptId | that attempt's per question detail | bearer, the attempt's own student | 404 not the caller's or not found | *(existing, reused for AC-5, not built here)* |

**Key invariants**:
- Every figure is scoped to the authenticated student (`StudentId` equals the `Guid UserId` from the JWT `NameIdentifier` claim). A query not so scoped is a security bug (foundation section 9, security.md section 7).
- "The score that counts" is the student's **latest submitted** attempt per quiz: the one whose `SubmittedAt` is set with the greatest `SubmittedAt`. **Do not gate on a state name.** An attempt moves `Submitted` to `Graded` to `Reviewable` within seconds, so a state name filter would drop nearly every finished attempt; `SubmittedAt.HasValue` is the stable predicate (the same rule as spec 0010).
- The per class standing is the average of per quiz percents (each = latest score over that quiz's total `Points`) over the quizzes the student has finished in that class, computed over one attempt per quiz (the latest). A quiz whose total points is zero yields no percent and is left out of the standing (guard the divide).
- Only submitted attempts appear. In progress and abandoned attempts are never result rows and never counted.
- Archived classrooms the student is enrolled in are still served.
- Nothing is stored. Every figure is computed on read, so no derived value can go stale.
- Currently enrolled classes only; a class the student has left drops off (see Follow-up).

**Security model**: Read only, scoped to the authenticated student. The endpoint takes no student id, so it can only ever return the caller's own attempts, which makes another student's results unreachable by construction, not merely by a check. The gate is `[Authorize]` with no role, because "your own results" is not a role question; a teacher who happened to take a quiz would see only their own attempts. An empty result is a plain 200 with an empty classrooms list (the student always owns their own results namespace), never a 404. No model call is made (per question feedback was already generated and stored at grading time), so security.md section 2 on LLM egress does not apply; section 7 on tenancy does and is the boundary: a student's academic performance is visible to that student. The index shows scores only; the stored feedback text is shown on the existing detail screen, which is already caller scoped.

**Configuration required**: none. No new environment variable, no new dependency, no model call.

**Critical test scenarios** (each maps to an acceptance criterion):
- Happy path: a student enrolled in two classes, having finished two quizzes in one and one in the other, opens My results and sees two classroom groups, each finished quiz as a row with score, percent, and date linking to its detail, and a per class standing. Verifies **AC-2**, **AC-4**, **AC-5**, **AC-6**.
- Failure and edge: a student who retried a quiz sees the latest submitted score and the older attempt does not skew that class's standing; a quiz with only an in progress attempt shows no result row (and at most a resume hint); a class the student is enrolled in but has finished nothing in shows as an empty group with a note, not omitted; a student who has finished nothing anywhere sees the calm page level empty state; an archived class the student is in still lists its results. Verifies **AC-2**, **AC-3**, **AC-4**, **AC-7**, **AC-8**, **AC-9**.
- Auth and permission: an unauthenticated call is 401; because the endpoint takes no student id, one student has no way to request another's results; a teacher token with no attempts gets an empty 200. Verifies **AC-1**, **AC-10**.

## Build plan

No migration (it reads existing tables), so task 1 is the read layer. Breadth first (foundation section 0): stand up the thinnest end to end thread first (the read plus the endpoint), then the screen, then the nav, then the tests and verify.

1. **Read layer and app service in the Assessment module**: the one shared read, "the student's latest submitted attempt per quiz across the classes they are enrolled in" (submitted = `SubmittedAt.HasValue`, greatest `SubmittedAt`), plus the per quiz percent and the per class standing. Reuse `IResultsReadRepository.GetSubmittedAttemptsAsync`. **Add an enrolled classrooms read that keeps archived classes** (AC-7; not `GetEnrolledAsync`, which drops them, see the data model cautions). **Extract the `latest submitted` reduction and the percentage / zero guard from `ClassroomResultsAppService`'s private helpers into a shared internal helper** both services use, so the two views cannot drift. New `MyResultsAppService` and the read DTOs. No migration. Satisfies **AC-3**, **AC-4**, **AC-6**, **AC-7**, **AC-10**; underpins **AC-2**, **AC-5**.
2. **The index endpoint** `GET /api/results/mine` on a new `MyResultsController`, scoped to the token principal, returning the grouped structure (an empty classrooms list when the student has finished nothing). The thin end to end thread. Satisfies **AC-1**, **AC-2**, **AC-7**, **AC-10**.
3. **The SPA screen** `MyResultsPage` in the existing `features/results/` slice (which already holds the single attempt screen it links into), mounted at route `results`, with a react-query hook, an api function, and a zod schema (the schema guards the wire contract the page tests mock away, the lesson from spec 0006). Per classroom sections (every enrolled class, a per group "nothing finished yet" note where empty, AC-2) with the standing and the finished quiz rows linking to the existing `results/:attemptId` detail; calm page level empty, loading, and error states; and the small in progress resume hint sourced from the existing `GetAvailableQuizzesAsync` (which already flags an in progress attempt and its id), not a new query. Built on the design system and `ui-rules.md` (calm, non punitive, misses framed for review, per `quiztin-design`). Satisfies **AC-5**, **AC-8**, **AC-9**; surfaces **AC-2**, **AC-4**, **AC-6**.
4. **Navigation**: a "My results" entry in the profile menu (next to Manage profile) and a link or card on the student dashboard. Satisfies **AC-11**.
5. **Tests, seed data, and verify**: backend integration tests on real Postgres (Testcontainers) that build their own fixtures (a student in two classes, several submitted attempts including a retried one, an in progress attempt, an archived class) to lock latest submitted, the per class standing, the only your own attempts scoping, archived inclusion, and the empty case; a small extension to the Development `DataSeeder` so the seeded student has finished quizzes across classes for a live walk; frontend tests for the page and its empty, loading, and error states, a schema test, and an axe pass; then `/check verify` the whole path against `verify.md`. Satisfies **AC-1** through **AC-11**.

## Consequences

**Positive**:
- Completes the student side of the results half (foundation section 7 #7): the "and progress" the landing page implies becomes real, symmetric to the teacher's spec 0010.
- The smallest possible surface: one read endpoint, one list page, no table, no migration, no write path change, and nothing new that can go stale.
- Reuses the 0010 read layer, the existing per attempt results screen, and the principal scoping pattern, so it stays consistent with the module. It is simpler than 0010: no cross module name lookup (own results) and no ownership 404 case (you always own your own results).

**Negative / tradeoffs**:
- No pagination in v1. The read returns all of a student's finished quizzes grouped by class. `Enrollment` has no term or end date and archived classes still count (AC-7), so this list grows with a student's whole enrolment history, not just the current term, which makes it less bounded than 0010's single classroom summary and monotonically growing over years of use. It is still small for any realistic student (classes number in the tens, not thousands), so v1 ships unpaginated as a conscious exception to the "paginate every list" rule; the Follow-up names capping to recent classes or terms as the first mitigation and treats this as ordinary long term growth, not a rare edge case.
- The standing is per class only; there is no single blended number across classes (a cross class average mixes different subjects and sizes and was judged less meaningful). A global progress number, if wanted, is a small later addition.
- Currently enrolled classes only. A class the student has left drops off their results even though the attempts remain (the mirror of 0010's departed student asymmetry). Noted in Follow-up.

**Neutral**:
- No new dependency, no new config, no model call.
- Adds read only endpoints and read DTOs to the Assessment module; the write (grading) side is untouched.
- The new list page lives in the existing `features/results/` slice, so the student results surface stays in one place (unlike 0010's separate `features/classroom-results/` slice, because here the per attempt detail screen is shared).

## Follow-up

- [ ] `GET /api/results/mine` is unpaginated and grows with the student's whole enrolment history (`Enrollment` has no term boundary and archived classes still count), so the list only grows over time. Cap it to recent classes or terms, or paginate, before that bites; this is expected long term growth, not a rare edge case.
- [ ] A single overall progress number across all classes (a blended or weighted standing), if wanted, is a small addition on top of the per class standings.
- [ ] Results for a class the student has left are **unlisted** here (the index reads current enrolment), though that attempt stays reachable by its own id through the existing detail endpoint, which checks only that the attempt is the caller's. Decide whether a departed class's past attempts should be listed again, the mirror of spec 0010's departed student question.
- [ ] Progress over time (trends across attempts and quizzes) stays deferred with the full multiple attempt history (foundation section 8); design it as its own slice if wanted.

## Rationale

Reasoning and the options weighed live in [rationale.md](rationale.md).
