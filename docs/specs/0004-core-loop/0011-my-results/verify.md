# Verify: My results and progress (student) · spec 0011

_Steps derived from spec 0011's eleven acceptance criteria. **Not built yet** (spec is Proposed). `/check verify` has never run against this spec. The backend path is automatable with a minted student JWT (the house convention, no passwords); the signed in UI walk is for a session or the `/check` harness. Written ahead of the build so `/develop` and `/check` have the contract._

## Automated coverage (green as of the build)
- **Backend integration tests** — `tests/Quiztin.Modules.Assessment.Tests/MyResultsTests.cs` (6 tests, real Postgres via Testcontainers): grouping by class with the latest submitted and a per class standing over two differently sized classes (AC-2, AC-4, AC-6), only the caller's own attempts returned (AC-1), a quiz with only an in progress attempt and an untaken quiz both excluded (AC-3), an archived class still served (AC-7), the empty result (AC-8), and an enrolled class with nothing finished shown as an empty group (AC-2). Run: `dotnet test --filter FullyQualifiedName~MyResultsTests`.
- **Endpoint test** — `tests/Quiztin.Api.Tests/MyResultsEndpointTests.cs` drives the real host pipeline: an anonymous `GET /api/results/mine` is 401 (AC-1). Run: `dotnet test tests/Quiztin.Api.Tests`.
- **Frontend tests** — `frontend/src/features/results/MyResultsPage.test.tsx` (5 tests: grouping and the per attempt links, the archived empty group, the page level empty state, the error state, and an axe pass) and `myResults.schemas.test.ts` (3 tests) for the wire contract the page test mocks away. Run: `npx vitest run src/features/results`.
- Backend **157 pass**, frontend **173 pass**.

## Setup
- [ ] Postgres up and the API in Development on `:8080`. Extend the seeder (spec 0011) so the **seed student** (Sam Carter, `22222222-…2`) has finished quizzes across **two** classes, so `GET /api/results/mine` has two groups to show and a standing per class.
- [ ] Mint a student JWT directly (no passwords, the house convention): HS256 over `JwtSettings__Secret`, claim `nameid` = the student Guid, `iss` / `aud` = `quiztin`. Mint a second student's token (any other Guid, ideally with their own finished attempts) to prove one student never sees another's results.
- [ ] The retried attempt and in progress edge cases (AC-4 latest, AC-9) are transient; lock them in `MyResultsTests`, or create them by hand for the UI walk.

## API (student token per step)
- [ ] `GET /api/results/mine` as the **seed student** → 200, results grouped by classroom, each group with the class name, archived flag, a standing percent, and a row per finished quiz (title, score out of the quiz total, percent, date, attempt id). → AC-2, AC-5, AC-6
- [ ] The same call as a **different student** → only that student's own results, never the seed student's. There is no student id in the request, so there is no parameter to point at another student. → AC-1
- [ ] `GET /api/results/mine` **unauthenticated** → 401. → AC-1
- [ ] A quiz the student can take but has not started does **not** appear; only submitted attempts do (confirm against the available quizzes list, which still shows the untaken quiz). → AC-3
- [ ] Give the seed student two submitted attempts on one quiz with different scores → the **latest submitted** shows, and the earlier does not skew that class's standing (locked by `MyResultsTests`). → AC-4, AC-6
- [ ] Give the seed student an in progress attempt (started, not submitted) → it is **not** a result row (locked by `MyResultsTests`); any resume affordance is a hint only. → AC-9
- [ ] Archive one of the student's classes, then repeat → its results still show (locked by `MyResultsTests`). → AC-7
- [ ] A class the student is enrolled in but has finished nothing in appears as an **empty group** with a "nothing finished yet" note, not omitted from the response. → AC-2
- [ ] A student who has finished nothing → 200 with an empty classrooms list (not a 404, not an error). → AC-8
- [ ] No migration was added and no results table exists (`git status`, and the read layer queries the existing attempt tables). → AC-10

## UI / manual (signed in as the seed student — the part for /check verify)
- [ ] Reach "My results" from the **profile menu** (next to Manage profile) and from the **student dashboard**. → AC-11
- [ ] The screen shows the finished quizzes grouped by class, each with the score and a per class standing. → AC-2, AC-5, AC-6
- [ ] Click a quiz row → the existing per attempt results screen (score, per question breakdown, stored feedback), the same one shown right after taking the quiz. → AC-5
- [ ] A student who has finished nothing sees a calm, encouraging empty state that points to available quizzes, not a blank or an error. → AC-8
- [ ] An in progress attempt shows at most a small resume hint, never a result row. → AC-9
- [ ] Signed out, the `results` route redirects to sign in. → AC-1

## Live drive result (this build)
Booted the host in Development against a throwaway Postgres (migrate + seed), minted a seed student JWT (no passwords, the house convention), and drove the endpoint. `GET /api/results/mine` returned 200 with two classroom groups: "Seed Classroom (Dev)" (standing 66.7%, Networking Basics 2/3) and "Study Group (Dev)" (standing 50%, Study Skills 1/2), every key camelCase and matching the frontend zod schema (classroomName, standingPercent, attemptId, submittedAt). An anonymous call was 401. So the two class seed data (spec 0011's `DataSeeder` extension), the archived inclusive enrolment read, and the on read grouping and standing were all confirmed end to end. 9 of 9 assertions passed.

The signed in UI walk (the browser steps below) is the part left for `/check verify`, per the house convention: a minted JWT backend step is automatable; a signed in browser flow is for a session or the `/check` harness.

## Acceptance-criteria coverage
- **AC-1** own results only, no student id in the request, 401 unauthenticated · **AC-2** grouped by classroom · **AC-3** submitted only, untaken stays in the available list · **AC-4** latest submitted counts · **AC-5** score, percent, date, links to the existing detail · **AC-6** per class standing, percentage normalized · **AC-7** archived class still served · **AC-8** calm empty state · **AC-9** in progress is not a result · **AC-10** read only, no table · **AC-11** reachable from the profile menu and the student dashboard.
