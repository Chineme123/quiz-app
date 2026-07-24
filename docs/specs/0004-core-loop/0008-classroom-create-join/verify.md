# Verify: Classroom create and join · spec 0008 · written 2026-07-23

_Steps derived from spec 0008's eleven acceptance criteria. `/check verify` runs these; `/test` locks the durable ones._

**Written after the fact.** This child shipped on 2026-07-19 with no verify file: the two children before it wrote their steps into the umbrella's `verify.md` and this one wrote none. The live end to end walk in the build's `progress-log.md` entry is the only record of it being verified, and its steps are reproduced below where they apply. **`/check verify` has never run against this spec independently.**

## Setup
- [ ] Postgres up and the API in Development, so the seeder creates the teacher, the student, the classroom (join code `SEED23`), and the published quiz.
- [ ] Two browser sessions, or one plus an incognito window: one signed in as the seed teacher, one as a **brand new** registered user (not the seed student, so nothing is pre enrolled).

## API (bearer token for the user named in each step)
- [ ] `POST /api/classrooms` as a **Teacher** with a name → 201, the classroom carries a 6 character join code and `TeacherId` is the caller. → AC-1
- [ ] The same call as a **Student** role → refused. → AC-1
- [ ] A blank name, whitespace only, or over 100 characters → 400, and nothing is created. → AC-1
- [ ] `GET /api/classrooms/owned` → only the caller's classrooms, each with student and quiz counts and its join code. → AC-2
- [ ] `GET /api/classrooms/by-code/{code}` → resolves to the class name for the preview, without enrolling anyone. → AC-4
- [ ] `POST /api/classrooms/join` with the code → 200 and an enrolment. Repeat it → succeeds again with **no** second enrolment row. → AC-3
- [ ] `GET /api/classrooms/enrolled` as that user → the class is listed. → AC-5
- [ ] `GET /api/classrooms/{id}` and `/students` as a **non owner** → 404, the same answer as an id that does not exist. → AC-11
- [ ] `PATCH /api/classrooms/{id}` (rename) and `POST /api/classrooms/{id}/regenerate-code` as a non owner → 404. As the owner → 200, and after regenerating, the **old code no longer resolves** through `by-code` or join. → AC-7, AC-11
- [ ] `GET /api/classrooms/{id}/students` → paginated, default page size 20, and a request for more than 50 is capped at 50. → AC-10
- [ ] `DELETE /api/classrooms/{id}/students/{studentId}` as the owner → the student is removed from the roster. → AC-7
- [ ] `POST /api/classrooms/{id}/leave` as the student → the enrolment goes. Repeat it → still succeeds (idempotent), and the student's past attempts and results are still readable. → AC-9

## The loop closure (the point of this slice, AC-6)
- [ ] As the new user, `GET /api/quizzes/available` **before** joining → the seeded quiz is absent.
- [ ] Join with `SEED23`, then call it again → the quiz now appears, and `POST /api/quizzes/{quizId}/start` returns **201**. The enrolment that gates taking (FR7) was created by a real person, not the seeder. → AC-6

## Archive (AC-8)
- [ ] `POST /api/classrooms/{id}/archive` as the owner → the class drops off the student's active list, its code stops resolving for join and preview, its quizzes leave the student's available list, and a start is refused with 400.
- [ ] An attempt the student had already begun before the archive still reads back. Nothing graded is destroyed.
- [ ] `POST /api/classrooms/{id}/unarchive` → the class, its code, and its quizzes all come back.

## UI / manual
- [ ] Sign in as a teacher → the post login home is the **teacher** dashboard: classes owned, each with name, student and quiz counts, and the join code. Sign in as a student → the **student** dashboard: classes joined, a join by code action, and a way to the available quizzes. → AC-2, AC-5
- [ ] A teacher with no classes, and a student who has joined none, each get an empty state that says what to do next rather than a blank page. → AC-2, AC-5
- [ ] Open `/join/{code}` **signed in** → the class name and a confirm control, never a silent join. → AC-4
- [ ] Open `/join/{code}` **signed out** → sign in first, then land back on the same join confirm. → AC-4
- [ ] Class detail as the owner: rename, reissue the code, archive and restore, and remove a student from the roster, each confirming before it acts. → AC-7, AC-8
- [ ] `npx vitest run src/features/classrooms` → green, including the axe pass. → AC-2, AC-5
- [ ] Keyboard only: reach create, join, and every management action, with a visible focus ring, and the `Dialog` traps focus and restores it on close.

## Acceptance-criteria coverage
- **AC-1** teacher only create with a validated name · **AC-2** teacher dashboard · **AC-3** idempotent join by code · **AC-4** `/join/{code}` previews then confirms, signed out or in · **AC-5** student dashboard · **AC-6** the enrolment is the one the take path checks · **AC-7** owner only rename, reissue, remove · **AC-8** archive is reversible and hides the class, its code, and its quizzes · **AC-9** leave is idempotent and preserves history · **AC-10** roster paginated 20 default, 50 max · **AC-11** every read and write scoped to the caller, non owner gets 404.
