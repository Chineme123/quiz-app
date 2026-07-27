# 0010. Teacher classroom results

**Date**: 2026-07-24
**Status**: Proposed

## Summary

A teacher opens a classroom they own and sees how the class is doing. For each quiz they see who has taken it, the class average, and which questions tripped people up; for each student they see a score per quiz and an overall standing, so it is easy to spot who is thriving and who needs a hand. It is read only: it adds no tables and writes nothing, it just reads and totals the attempt data that already exists, scoped to the owning teacher. This is the last child of the core loop (umbrella 0004).

## Requirements

**User stories**:
- As a teacher, I want a classroom's results in one place, so I can tell at a glance who is doing well and who is falling behind without opening each attempt by hand.
- As a teacher, I want to see which questions the class got wrong, so I know what to reteach.
- As a teacher, I want to see who has not taken a quiz yet, so I can chase them.

**Acceptance criteria** (the contract, each IDed and independently checkable):
- **AC-1**: Only the teacher who owns the classroom can read its results. A non owner, and a classroom id that does not exist, both get **404** (existence never leaks). A caller with the Student role is refused.
- **AC-2**: A classroom results summary lists each quiz that is published or has any attempt (never published drafts are excluded). Each row shows the quiz title, how many enrolled students have a submitted attempt (a completion count), and the class average score over those submitted attempts.
- **AC-3**: A per quiz view shows, for one quiz, every enrolled student (by display name, AC-13) with the score of their **latest submitted** attempt (as points out of the quiz's total), or a "Not taken" or "In progress" marker. Non takers and in progress attempts are excluded from the average.
- **AC-4**: The per quiz view shows, for each question, the fraction that answered it correctly, counted over one attempt per student (each student's latest submitted), so a retried attempt never counts twice (per question difficulty).
- **AC-5**: A per student roll up shows, for the classroom, each enrolled student's score on each quiz plus an overall standing. Because quizzes have different point totals, each quiz score is first turned into a percentage of that quiz's total points, and the overall standing is the average of those percentages over the quizzes the student has taken, so quizzes of different sizes compare fairly.
- **AC-6**: From a student's score on a quiz, the teacher can open that attempt's per question detail (the same breakdown the student sees on their own results screen), scoped to the owning teacher.
- **AC-7**: When a student has more than one submitted attempt on a quiz, the latest submitted attempt is the one that counts, everywhere.
- **AC-8**: If a student has any submitted attempt, their latest submitted score shows, even if they also have a newer still open attempt (the submitted result stands, per AC-7). "In progress" shows only when there is an open attempt and no submitted one; "Not taken" when there is neither. In progress and not taken are excluded from every average.
- **AC-9**: Results are served for archived classrooms too (owner only), because archiving preserves history (spec 0008).
- **AC-10**: The per student lists are paginated with a default page size of 20 and a hard maximum of 50 (the list convention of specs 0006 and 0008).
- **AC-11**: The feature is read only. It adds no table and performs no write; every figure is computed on read from the existing attempt data, scoped to the classroom.
- **AC-12**: The SPA gives the teacher a classroom results screen reachable from the class page, a per quiz results view, and the per student roll up, all behind sign in and teacher ownership. A signed in student who reaches a results URL gets the calm "we couldn't find that" state (the 404 reads as null, the house convention), not a distinct "forbidden" screen.
- **AC-13**: Every student row shows the student's display name, resolved in process from the Identity module (a read only lookup that turns a set of student ids into names), falling back to the student's email when no display name is set. A row is never a bare id.

## Decision

**Chosen option**: Option 1: aggregate on read, no projection.

Serve the teacher's classroom results by querying and totalling the existing `QuizAttempt`, `QuizAnswer`, and `Enrollment` data live in the Assessment module, scoped to the owning teacher's classroom, with no new table and no change to the write (grading) path.

**Implementation skills**: `quiztin-design` (`.claude/skills/quiztin-design/`, the results screens' tokens and voice, and the `ui-rules.md` for a calm, non punitive presentation)

## Feature design

**Data model sketch**: **No new entities and no migration.** The read totals existing entities:
- `Classroom` (`TeacherId` for ownership, `ArchivedAt`), `Enrollment` (`StudentId` in a `ClassroomId`, the roster), `Quiz` (`ClassroomId`, `CreatedByTeacherId` for ownership, `IsPublished`, `Title`), `Question` (per quiz, `Points`), `QuizAttempt` (`StudentId`, `QuizId`, `SubmittedAt`, `TotalScore`), `QuizAnswer` (per question correctness on an attempt).
- **One shared read query underpins every view: "the latest submitted attempt per (student, quiz) in a classroom".** An attempt counts as submitted when its `SubmittedAt` is set (see invariants), and every aggregate is computed over that one-per-student set, so the summary, the per quiz view, and the roll up cannot disagree.
- Values are derived on read, never stored: per student latest submitted score; per quiz completion count (distinct enrolled students with a submitted attempt) and class average (over the one-per-student set); per question fraction correct (over the same set); per student overall standing (the average of per quiz percentages, each score over that quiz's total `Points`).
- **Student names come from the Identity module (the first cross module read).** `Enrollment` carries only a `StudentId` (a Guid); the display name lives in Identity's `Profile`, and the email in `AuthUser`. A narrow, read only, in process lookup (a contract Identity exposes, Assessment consumes) resolves a set of student ids into `{ displayName, email }`. Spec 0008 flagged the mirror image gap (the teacher's name) as a follow up; this contract is the place that resolves both.
- New read only DTOs (no persistence): `ClassroomResultsSummaryDto`, `QuizResultsDto` (per student rows plus per question difficulty), `StudentRollupDto`. Drill down reuses the existing per attempt result DTO.

**State transitions**: none. The feature is read only.

**API surface** (all owner scoped; the authenticated teacher must own the classroom, or the quiz's classroom; non owner and not found both → 404):

| Endpoint | Method | Key inputs | Key outputs | Auth | Key errors |
|---|---|---|---|---|---|
| `/api/classrooms/{classroomId}/results` | GET | classroomId | per quiz summary rows (title, completion count, class average); classroom meta | bearer, owning teacher | 404 not owner or not found |
| `/api/classrooms/{classroomId}/results/students` | GET | classroomId, page, pageSize | paginated per student roll up (score per quiz, overall standing) | bearer, owning teacher | 404 |
| `/api/quizzes/{quizId}/results` | GET | quizId, page, pageSize | per question fraction correct; paginated per student rows (latest submitted score, or Not taken / In progress) | bearer, quiz's `CreatedByTeacherId` | 404 |
| `/api/quizzes/{quizId}/results/students/{studentId}` | GET | quizId, studentId | that student's latest submitted attempt, per question detail (drill down) | bearer, quiz's `CreatedByTeacherId` | 404 not owner / not found / student has no submitted attempt |

**Key invariants**:
- Every figure is scoped to the classroom the authenticated teacher owns. A query that is not so scoped is a security bug (foundation §9, `security.md` §4 and §7).
- "The score that counts" is the student's **latest submitted** attempt: the one whose `SubmittedAt` is set (it is stamped once at submit and never cleared) with the greatest `SubmittedAt`. **Do not gate on a state name.** An attempt moves `Submitted → Graded → Reviewable` within seconds of submitting (grading is synchronous, feedback follows in the background), so a state-name filter would drop nearly every finished attempt; `SubmittedAt.HasValue` is the stable predicate. In progress and abandoned attempts have no `SubmittedAt` and never count.
- Every aggregate (the class average, the per question fraction correct, the overall standing) is computed over **one attempt per student** (that student's latest submitted), never over all attempts, so a student who retried does not count more than once. This is the same set AC-7 fixes, applied to the totals, not just the displayed cell.
- Scores are only compared across quizzes after normalizing to a percentage of each quiz's total `Points` (AC-5). A raw `TotalScore` is a point sum with no fixed scale, so averaging raw totals across differently sized quizzes would be meaningless.
- Averages count only currently enrolled students; non takers and in progress attempts are excluded, not counted as zero.
- Nothing is stored. All figures are computed on read, so no derived value can go stale.

**Security model**: Read only, owner scoped. The authenticated `Guid UserId` (JWT `NameIdentifier`) must equal the classroom's `TeacherId` (classroom endpoints) or the quiz's `CreatedByTeacherId` (quiz endpoints; this is the field every existing quiz endpoint checks, and it equals the classroom's `TeacherId`, set together at quiz creation). The ownership check runs first, before any student row is touched. Non owner and not found both return **404**, so ownership never leaks (`code-standards.md` §5). A Student role caller has no route here; their own results are UC9. The cross module name lookup (AC-13) returns names only for the ids the caller's own classroom already contains, so it cannot be used to probe identities outside the tenant. The teacher does see their own students' names and scores: that is the legitimate purpose of a classroom results view (classroom management), it stays within the tenant, and no student data leaves to any third party or the LLM (this feature makes no model call), so `security.md` §2 on LLM egress does not apply; §4 and §7 on tenancy do, and are the boundary.

**Configuration required**: none. No new environment variable, no new dependency, no model call.

**Critical test scenarios** (each maps to an acceptance criterion):
- Happy path: a teacher with a seeded classroom opens results, sees each quiz's completion and average, opens a quiz and sees per student rows **by name** and per question fraction correct, and drills into one student's attempt. Verifies **AC-2**, **AC-3**, **AC-4**, **AC-6**, **AC-13**.
- Failure and edge (the aggregation rules the cross check flagged): a student with two submitted attempts shows the latest one's score everywhere, **and their retried attempt does not skew the class average or the per question fraction** (aggregates are one per student); a student with a submitted score plus a newer open attempt still shows the submitted score; a student with only an in progress attempt shows "In progress" and is left out of the average; a quiz nobody has taken shows zero completion and no average. Verifies **AC-4**, **AC-7**, **AC-8**.
- Auth and permission: a teacher who does not own the classroom, and a student, both get 404 on every results endpoint (a student sees the calm not found state), and a not found classroom id is indistinguishable from a not owned one. Verifies **AC-1**, **AC-12**.

## Build plan

This feature has **no migration** (it reads existing tables), so task 1 is the read layer, not a schema change. Breadth first (foundation §0): stand up the thinnest end to end thread first (the classroom summary), then thicken.

1. **Read layer in the Assessment module**: the one shared query, "the latest submitted attempt per (student, quiz) in a classroom" (submitted = `SubmittedAt.HasValue`, greatest `SubmittedAt`), plus the read queries built on it (classroom ownership check, roster, per quiz completion and average, per question fraction correct, per student standing), an application read service, and the read DTOs. **Also the cross module name lookup**: a narrow read only contract Identity exposes (student ids → `{ displayName, email }`) that Assessment consumes in process, wired in `AssessmentModule`/`IdentityModule`. No migration. Satisfies **AC-11**, **AC-13**; underpins **AC-2** through **AC-8**.
2. **Classroom results summary endpoint** `GET /api/classrooms/{classroomId}/results`, owner scoped (404 for a non owner), returning per quiz rows (completion, average) for published or attempted quizzes. The thin end to end thread. Satisfies **AC-1**, **AC-2**, **AC-9**.
3. **Per quiz results endpoint** `GET /api/quizzes/{quizId}/results`: per student latest submitted score, or Not taken / In progress, plus per question fraction correct; paginated student list. Satisfies **AC-3**, **AC-4**, **AC-7**, **AC-8**, **AC-10**.
4. **Per student roll up endpoint** `GET /api/classrooms/{classroomId}/results/students`: each student's score per quiz plus an overall standing, paginated. Satisfies **AC-5**, **AC-10**.
5. **Drill down endpoint** `GET /api/quizzes/{quizId}/results/students/{studentId}`: that student's latest submitted attempt per question detail, reusing the existing result computation, owner scoped. Satisfies **AC-6**.
6. **SPA screens** in a new `features/classroom-results/` slice (distinct from the existing `features/results/`, which is UC9's student per attempt view, so no collision): a classroom results screen reachable from the class page (the per quiz summary, with a way into each quiz and the roll up), a per quiz results view (per student by name plus per question difficulty), the per student roll up, and drill down into an attempt. Behind `RequireAuth` and teacher ownership; a student who reaches a results URL gets the house calm not found state (404 reads as null), not a bespoke "forbidden" screen. Built on the design system and `ui-rules.md` (calm and non punitive: misses framed as "to review", per `quiztin-design`). Satisfies **AC-12**, **AC-13**; surfaces **AC-2** through **AC-8**.
7. **Tests, seed data, and verify**: backend integration tests on real Postgres (Testcontainers) that build their own fixtures (a classroom, two students, several submitted attempts including a retried one, and one in progress attempt) to lock the aggregation rules, the dedup, and the 404 scoping; a small extension to the Development `DataSeeder` (a second student and a few attempts) so the live walk has something to show; frontend tests for the screens and their empty, loading, and error states; then `/check verify` the whole teacher results path against `verify.md`. Satisfies **AC-1** through **AC-13**.

## Consequences

**Positive**:
- Completes the core loop (umbrella 0004): the teacher side of "see who is thriving and who needs a hand", which the landing page already promises, becomes real.
- The smallest possible surface: no table, no migration, no write path change, and nothing new that can go stale (every figure is computed on read).
- Reuses the existing attempt and result data and the owner scoping pattern, so it stays consistent with the rest of the module.

**Negative / tradeoffs**:
- Aggregate on read runs a few grouped queries over the attempt tables per view. At classroom scale (tens of students, a handful of quizzes) that is fine, but a very large class, or a later cross classroom analytics view, would want a projection. Noted in Follow-up, not built now.
- The teacher sees individual student scores by name. That is intended for a classroom tool, but it is real personal academic data, so the tenancy scoping must be exact or it leaks across classes.
- **This introduces the first cross module read** (Assessment reading student names from Identity). It is in process and narrow (ids to names, read only), but it is a new dependency direction to keep honest: Identity owns the contract, Assessment consumes it, and the contract returns names only for ids the caller's own classroom already holds.
- Departed students (who left the class but keep past attempts) are out of scope for the roster based views (they show current enrollment only). One asymmetry to accept: the drill down URL is keyed by quiz and student id, so a departed student's attempt could still resolve there even though they are absent from the roster views. Noted in Follow-up.

**Neutral**:
- No new dependency, no new config, no model call.
- Adds read only endpoints and read DTOs to the Assessment module; the write side (grading) is untouched.

## Follow-up

- [ ] If cross classroom or school wide analytics is wanted later, revisit aggregate on read and consider a results projection updated on the grading event (the runner up option here).
- [ ] The cross module name lookup built here (student ids to names) is the same contract spec 0008 needed for the teacher's name (its deferred follow up). Once this ships, close that 0008 follow up by pointing the classroom lists at the same lookup.
- [ ] Decide how to present departed students' past attempts (currently excluded from the roster views; still reachable through the drill down URL), if it becomes a real need.
- [ ] CSV export of results (the original UC13) is deferred; design it as its own slice when wanted.
- [ ] Per question difficulty reads correct or incorrect from `QuizAnswer`. If partial credit question types are added later, revisit the "fraction correct" definition.

## Rationale

Reasoning and the options weighed live in [rationale.md](rationale.md).
