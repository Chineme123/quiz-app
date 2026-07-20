# 0008. Classroom create and join (the dependency root)

**Date**: 2026-07-19

## Summary

This is the child of the core loop umbrella (0004) that lets a real teacher stand up a classroom and a real student join it, so enrolment stops being something only the seeder can create. Today the `Classroom` and `Enrollment` models exist and the take quiz path already refuses a student who is not enrolled (FR7), but there is no way through the product to create a classroom or to enrol, so the whole loop only works on seeded data. This slice adds the write side those reads already assume: a teacher creates a classroom and gets a short join code plus a shareable link, any signed in user joins by code or link, and both sides get a role aware home (a teacher dashboard and a student dashboard) in place of today's empty post login landing. A teacher also gets full management of their classes: rename, archive (reversible), and regenerate the code; a student can leave a class. With this in, a person can walk register, join, take, results end to end without touching the seeder.

## Requirements

**User stories**:
- As a teacher, I want to create a classroom and share a short code or a link, so my students can enrol themselves without me wiring anything by hand.
- As a student, I want to join a class by typing a code or opening a link, so I can reach the quizzes my teacher set.
- As a teacher, I want a home that lists my classes with their codes and their student and quiz counts, so I can manage them at a glance.
- As a student, I want a home that lists the classes I have joined and lets me join another, so I always know where my quizzes live.
- As a teacher, I want to rename, archive, or re issue the code of a class, so a leaked code or a renamed course does not force me to start over.

**Acceptance criteria** (the contract, each IDed and independently checkable):

- **AC-1**: A user whose JWT role claim is `Teacher` can create a classroom with a name (trimmed, 1 to 100 characters, not blank). The classroom is stored with `TeacherId` set to the authenticated `Guid UserId` from the JWT `NameIdentifier` claim, never a client supplied id, and is issued a unique join code (6 characters, uppercase, ambiguous characters removed). A non teacher requesting create is refused with 403.
- **AC-2**: The post login home is role aware. A teacher lands on a teacher dashboard that lists the classrooms they own, each row showing the name, the student count, the quiz count, the join code, and a control to copy the join link, plus a create action and an empty state when they own none. The list is scoped to the authenticated `UserId`.
- **AC-3**: Any authenticated user can join a classroom by entering its code. Joining is idempotent: entering the code of a class the user already belongs to succeeds without creating a second enrolment. An unknown or archived code returns a not found result. Joining a class the user owns is refused with a clear conflict message. On success an `Enrollment` row is created for the authenticated `UserId`.
- **AC-4**: A join link has the form `/join/{code}`. Opened while signed in, it shows the class name (resolved from the code) and a confirm control before enrolling, never a silent join. Opened while signed out, it routes through `/sign-in` and then resumes to the same join screen, so the link works for a brand new user in one pass.
- **AC-5**: A student lands on a student dashboard that lists the classes they have joined, offers a join by code action, and links to their available quizzes, with an empty state that prompts them to join a class. The list is scoped to the authenticated `UserId`.
- **AC-6**: Enrolment created here is the same enrolment the take path already checks. After a student joins a class that has a published, in window quiz, that quiz appears in their available quizzes list and they can start it, with no seeded row involved. This is FR7 becoming real through the product.
- **AC-7**: The owning teacher, and only they, can rename a classroom (name re validated 1 to 100), regenerate its join code (the old code and link stop resolving, a new unique one is issued), archive or unarchive it, and remove an enrolled student (needed because join is open, so a leaked or mistyped code can enrol someone the teacher must be able to evict; removing a student deletes only the enrolment, never their past attempts or results). Any of these attempted by a non owner returns 404, so the class is never revealed to someone who does not own it.
- **AC-8**: Archiving a classroom removes it from students' active class lists, stops its code resolving for join or preview, and stops its quizzes both appearing in the available quizzes list and being startable, while preserving every enrolment, quiz, and past attempt and result. Enforcement is on the take side: the available quizzes read (`QuizRepository.GetAvailableForStudentAsync`) and the start gate (`TakeQuizFacade.StartQuizAsync`) both exclude a quiz whose classroom is archived. An attempt already in progress when the class is archived is left to finish (consistent with spec 0006's window precedent): archiving blocks new starts, not an open attempt. Unarchiving restores all of that.
- **AC-9**: A student can leave a class they joined. The enrolment is removed (repeating the leave is idempotent), their past attempts and results are preserved, and the class drops off their dashboard.
- **AC-10**: The owner roster (the enrolled students of a class) is paginated with a default page size of 20 and a hard maximum of 50 (matching spec 0006's list convention). The owned and enrolled class lists are each scoped to the authenticated user.
- **AC-11**: Every classroom and enrolment read and write is scoped by the authenticated `Guid UserId` from the JWT `NameIdentifier`. Owner scoped resources deny a non owner with 404, participant scoped reads return only the caller's own rows, and no query against the `classrooms` or `enrollments` tables runs unscoped.

## Decision

**Chosen option**: Option 2: Short join code on the classroom, with the link wrapping the same code, full management, and role aware dashboards.

Add a unique short join code to `Classroom` (the link is just `/join/{code}` around it, one secret to rotate), gate create to the `Teacher` role while leaving join open to any authenticated user, model delete as a reversible archive so student history is never destroyed, and make the post login landing a role aware dashboard. The two per user class lists (owned and enrolled) drive the two dashboards; the teacher gets create, rename, archive, unarchive, regenerate, and a paginated roster; the student gets join and leave.

**Implementation skills**: `quiztin-design` (the project's design system skill, `.claude/skills/quiztin-design/`) for the dashboards, the create and join screens, and all class management surfaces.

## Rationale

The join code lives on the classroom, not in a separate invite entity, because the code is the whole capability. One field with a unique index answers both "does this code resolve" and "to which class", the link is a thin wrapper around it (`/join/{code}`), and regenerating the code rotates the code and the link together with no second thing to track. A separate token table or an emailed invite would add moving parts this solo, no email project does not need yet.

Create is gated to the `Teacher` role because every user is fixed as `Student` or `Teacher` at registration and the JWT already carries the role claim (`JwtTokenService` adds `ClaimTypes.Role`), so the gate is a free read of the token, not a new lookup. Join stays open to any authenticated user because the code itself is the gate; this keeps the door simple and lets a teacher preview a colleague's class if they ever have the code, while a real cross tenant leak is still blocked by the owner only management rules. Delete is a reversible archive, not a row delete, because a class owns quizzes and those quizzes own graded student attempts; a hard delete would destroy real student result history, which foundation and the security posture treat as data to protect. An explicit `ArchivedAt` timestamp is the recommended shape over a soft delete flag, so queries stay honest and the code is never freed for reuse. The dashboards replace the empty post login landing because the take side already has screens (0005, 0006) while the setup side has none, so this is the missing half of the loop, not a nice to have. Tenancy scoping follows the umbrella's shared contract 3 (foundation section 9, security.md sections 4 and 7): every read and write is keyed to the JWT `UserId`, and owner scoped misses return 404 so existence never leaks, matching the take path's "not yours reads as not found" rule (0006 AC-5).

Showing the teacher's display name on a student's view of a class is deliberately left out of this slice: the teacher's name lives in the Identity module and the Assessment module holds only `TeacherId` as a plain cross module Guid (spec 0007), so surfacing it would mean a cross module read. The class name the teacher types carries enough meaning for v1; the cross module lookup is a follow up.

## Feature design

**Data model sketch** (Assessment module, `quiz` schema):

- **Classroom** (extend the existing entity)
  - `Id` Guid, primary key (exists)
  - `TeacherId` Guid, required, indexed, plain cross module id (no cross schema FK, per spec 0007) (exists)
  - `Name` string, required, 1 to 100 characters (exists)
  - `JoinCode` string, required, **unique index** (new): 6 characters, uppercase, ambiguous characters removed
  - `ArchivedAt` DateTime?, null means active (new)
  - `CreatedAt` DateTime, set at creation (new), for stable dashboard ordering
  - `Quizzes` 1 to many `Quiz` (exists)
- **Enrollment** (extend the existing entity)
  - `Id` Guid, primary key (exists)
  - `StudentId` Guid, required, indexed, plain cross module id (the enrolled user) (exists)
  - `ClassroomId` Guid, **real FK to `Classroom.Id`** within the module (new: today it is a plain Guid with no FK)
  - `EnrolledAt` DateTime (exists)
  - Unique (`StudentId`, `ClassroomId`): **already exists today** (`QuizDbContext` line 137), so at most one enrolment per user per class is already enforced; this slice relies on it for idempotent join, it does not add it
  - Index (`ClassroomId`) (new): roster and count reads

**State transitions**:
- Classroom: `active` (ArchivedAt null) archives to `archived` (ArchivedAt set), and unarchives back to `active`. No other states.
- Enrollment: does not exist, then `enrolled` on join, then removed on leave. Re joining or re leaving is a no op that lands in the same state.

**API surface** (all under the Assessment module; `bearer` = any authenticated user):

| Endpoint | Method | Key inputs | Key outputs | Auth | Key errors |
|---|---|---|---|---|---|
| `/api/classrooms` | POST | `name`:string (req) | `id`, `name`, `joinCode`, `createdAt` | role Teacher | 400 invalid name, 403 not a teacher |
| `/api/classrooms/owned` | GET | paging (opt) | `[{id, name, joinCode, studentCount, quizCount, archivedAt, createdAt}]` | role Teacher | none |
| `/api/classrooms/enrolled` | GET | none | `[{id, name}]` | bearer | none |
| `/api/classrooms/{id}` | GET | `id` | owner: `{id, name, joinCode, archivedAt, quizCount, studentCount}`; enrolled: `{id, name}` | owner or enrolled | 404 not yours and not enrolled |
| `/api/classrooms/{id}/students` | GET | `id`, `page`, `pageSize` | `{students:[{studentId, enrolledAt}], total}` | owner | 404 not yours |
| `/api/classrooms/{id}/students/{studentId}` | DELETE | `id`, `studentId` | 204 | owner | 404 not yours; 204 if not enrolled (idempotent) |
| `/api/classrooms/by-code/{code}` | GET | `code` | `{classroomId, name, alreadyEnrolled, isOwner}` | bearer | 404 unknown or archived |
| `/api/classrooms/join` | POST | `code`:string (req) | `{classroomId, name}` | bearer | 404 unknown or archived, 409 own class |
| `/api/classrooms/{id}/leave` | POST | `id` | 204 | bearer (self) | 204 even if not enrolled (idempotent) |
| `/api/classrooms/{id}` | PATCH | `name`:string (req) | `{id, name}` | owner | 400 invalid, 404 not yours |
| `/api/classrooms/{id}/archive` | POST | `id` | `{id, archivedAt}` | owner | 404 not yours |
| `/api/classrooms/{id}/unarchive` | POST | `id` | `{id}` | owner | 404 not yours |
| `/api/classrooms/{id}/regenerate-code` | POST | `id` | `{id, joinCode}` | owner | 404 not yours |

The join link is built on the client from `window.location.origin` plus `/join/{code}`, so the server stores and returns only the code and needs no public base URL configured.

**Key invariants**:
- `JoinCode` is unique across all classrooms, active and archived alike; archiving never frees a code for reuse. Generation picks a random code and inserts under the unique index, retrying on the rare collision, so two classrooms can never share a code.
- At most one `Enrollment` per (`StudentId`, `ClassroomId`), enforced by the already existing unique index. Join is therefore idempotent even under a race: the handler maps a unique constraint violation (two concurrent joins on the same pair, which really can fire since the persistence tests run against real Postgres) to the same success response, it does not surface a 500. A double submit or a retry cannot duplicate.
- A classroom's own `TeacherId` never appears among its enrolments (joining a class you own is refused).
- Only a `Teacher` role user can own a classroom (create is role gated).
- An archived classroom (`ArchivedAt` set) is not joinable, not previewable by code, and its quizzes neither list nor start for a student, enforced on the take side in `GetAvailableForStudentAsync` and `StartQuizAsync`; yet all its rows persist and an attempt already open is left to finish.
- Every read and write on `classrooms` and `enrollments` is scoped by the JWT `UserId`; owner scoped misses return 404 (existence never leaks). This is the umbrella's shared contract 3.

**Security model**:
- **Create**: role `Teacher` only (JWT role claim).
- **Owner only** (rename, archive, unarchive, regenerate code, owned list, owner detail, roster): the caller's `UserId` must equal the classroom's `TeacherId`; otherwise 404, never 403, so a non owner cannot tell the class exists.
- **Any authenticated user** (join, by code preview, leave, enrolled list): scoped to the caller's own enrolments; the join code is the capability to find a class.
- The join code is a low sensitivity capability, not a secret, but the code space (6 characters over a 31 character alphabet, about 887 million combinations) still deserves a per user rate limit on join and by code so it cannot be enumerated. See Follow up if the app has no rate limiting yet.
- No student PII and no classroom data leave the app; the model (Claude) is not involved in this slice, so the umbrella's data minimization contract has nothing to send here.

**Configuration required**: none. No new environment variables, secrets, or third party credentials. The join link is composed client side.

**Critical test scenarios** (each maps to an acceptance criterion):
- Happy path: a teacher creates a class, copies the link, a second user opens `/join/{code}`, confirms, and is enrolled; the class then shows on the student dashboard, verifies **AC-1**, **AC-3**, **AC-4**, **AC-5**.
- Loop closes: after that join, a published quiz in the class appears in the student's available quizzes and starts, with no seeded enrolment, verifies **AC-6**.
- Idempotent join: submitting the same code twice, or two concurrent joins, yields exactly one enrolment, verifies **AC-3**.
- Auth and ownership: a non teacher creating a class gets 403; a teacher renaming, archiving, or regenerating a class they do not own gets 404, verifies **AC-1**, **AC-7**, **AC-11**.
- Archive preserves history and blocks starts: archiving a class with a graded attempt hides it, blocks its code, and stops a student both starting and seeing its quizzes in the available list, while the graded attempt and its result still read back; unarchive restores the class, verifies **AC-8**.

## Build plan

Ordered as the umbrella's breadth first stance implies (foundation section 0), but standing the core create then join thread up before the management polish, so the loop is walkable early. The data model migration is task 1.

1. Migration and model: add `JoinCode` (unique), `ArchivedAt`, `CreatedAt` to `Classroom`; make `Enrollment.ClassroomId` a real FK and add unique (`StudentId`, `ClassroomId`) plus the `ClassroomId` index. Regenerate the Assessment `InitialCreate` line as the module's migrations require. Satisfies **AC-1**, **AC-3**, **AC-10**.
2. Domain and application: a join code generator (6 characters, alphabet `ABCDEFGHJKMNPQRSTUVWXYZ23456789`, insert under the unique index and retry on the rare collision); classroom create, rename, archive, unarchive, regenerate code, and remove a student, all with the owner and role guards; enrolment join and leave with idempotency, the own class refusal, and the concurrent join mapped from a unique constraint violation to the same success (not a 500); the owned and enrolled queries scoped to the JWT `UserId`; and archive enforcement on the take side, filtering `QuizRepository.GetAvailableForStudentAsync` and the start gate in `TakeQuizFacade.StartQuizAsync` on `Classroom.ArchivedAt == null` so an archived class neither lists nor starts its quizzes (an open attempt is left to finish). Satisfies **AC-1**, **AC-3**, **AC-7**, **AC-8**, **AC-9**, **AC-11**.
3. API: a `ClassroomsController` exposing the table above, with role and owner authorization, the 404 on owner scoped misses, the paginated roster, and the teacher remove student endpoint. Extract the repeated `GetCurrentUserId()` (today copy pasted in `QuizController` and `QuizAttemptsController`) into a shared base so this third controller does not duplicate it a third time. Satisfies **AC-1**, **AC-2**, **AC-3**, **AC-5**, **AC-7**, **AC-10**, **AC-11**.
4. Frontend core loop: a role aware `/dashboard` redirect after sign in; a teacher dashboard (owned list with counts, code, copy link, create); a student dashboard (enrolled list, join by code, link to available quizzes); and the `/join/{code}` link flow that previews the class, resumes through `/sign-in` when signed out, and confirms. Satisfies **AC-2**, **AC-4**, **AC-5**, **AC-6**.
5. Frontend management: class detail for the owner (paginated roster with a remove student control, rename, archive, unarchive, regenerate code with copy link) and the student leave action. Satisfies **AC-7**, **AC-8**, **AC-9**.
6. Tests and end to end verify: backend tests for role gating, ownership 404, join idempotency (including a concurrent join yielding exactly one enrolment against real Postgres), own class refusal, teacher remove student, archive both preserving history and blocking a start plus hiding the quiz from the available list, and tenancy scoping; frontend tests for the two dashboards, the join and link flow, and accessibility; then a full walk proving a created class plus a real join lets the student reach and start the quiz. Satisfies **AC-1** through **AC-11**.

## Consequences

**Positive**:
- FR7 becomes real through the product: a human can create a class and enrol, so the loop no longer depends on the seeder, and the earlier children (0005, 0006) get real preconditions.
- The teacher side of the app exists for the first time; the empty post login landing becomes a useful role aware home.
- Delete as archive means no teacher action can destroy graded student history, which keeps the app safe to hand to real users.

**Negative / tradeoffs**:
- This is the largest child so far (both dashboards, full management, the link flow), so it lands over more build steps than a thin slice; the core create then join thread is sequenced first to reduce that risk.
- Archive that never frees a join code means the code space slowly fills; at 887 million combinations this is a non issue for a long time, but it is a real one way cost of not reusing codes.
- Owner scoped misses returning 404 instead of 403 is deliberately less informative to the caller, traded for not leaking that a class exists.

**Neutral**:
- Making `Enrollment.ClassroomId` a real FK tightens integrity within the module but means an enrolment can no longer point at a missing class, so the seeder and any test fixtures must create the class first.
- The student facing views show the class name but not the teacher's name this slice, to avoid a cross module read (spec 0007); adding the name later is a bounded follow up.
- Joining stays open to any authenticated user, so an enrolled participant is defined by enrolment, not by global role; the take path already keys on enrolment, so this is consistent.

## Follow-up

- [ ] Add a per user rate limit on `POST /api/classrooms/join` and `GET /api/classrooms/by-code/{code}` (ASP.NET Core rate limiting) so the code space cannot be enumerated. This slice ships open join before the throttle; the interim risk is bounded because joining requires an authenticated account (not an anonymous surface), but the throttle should follow closely rather than drift. If the app has no rate limiting middleware yet, that is the prerequisite; note it in `library-docs.md` when added.
- [ ] Show the teacher's display name on student facing class views. This needs a cross module read from Identity or a denormalized name on the classroom; both are deferred by spec 0007's plain Guid boundary.
- [ ] Reconcile the context system for this child: cite 0008 from `foundation.md` (the core loop children) and update `build-graph.md` (UC2 and UC3 move from model only to built) and `progress-log.md` when the slice lands.
- [ ] Confirm whether the student dashboard should also show a per class quiz count; left off here to avoid a cross module or extra aggregate read until the shape of the student home settles.

## References

**Project sources**:
- Umbrella spec 0004 (the core loop), which names this as the "(planned) Classroom create and join" child and defines shared contract 3, tenancy scoping.
- Spec 0007 (modular monolith), the rule that cross module references stay plain indexed Guids with no cross schema FK, which fixes `TeacherId` and `StudentId` as plain Guids and defers the teacher name.
- Spec 0006 (take quiz screen) AC-5, the "not yours reads as not found" pattern reused here for owner scoped 404s.
- `foundation.md` sections 0 (breadth first), 9 (tenancy from the JWT `UserId`); `security.md` sections 4 and 7 (non bypassable per user scoping); `JwtTokenService` (the role claim this design gates on).
- The existing `Classroom` and `Enrollment` entities and `TakeQuizFacade` and `QuizAppService`, the read side that already consumes enrolment.

**Practices & standards**:
- Explicit `archived_at` timestamp over a soft delete flag, so queries stay honest and unique constraints are not polluted.
- Idempotency from day one for mutations (join and leave), enforced by a unique constraint rather than a read then write.
- Pagination on the unbounded list (the roster), with a default page size and a hard cap.
- Deny by not revealing (404 over 403) for owner scoped resources, so existence does not leak across tenants.
