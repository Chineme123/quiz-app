# 0007. Modular monolith — verification

Run from the repo root. The local .NET 10 SDK lives at `~/.dotnet`
(`export DOTNET_ROOT="$HOME/.dotnet"; export PATH="$HOME/.dotnet:$PATH"`).

> **Re-verified 2026-07-24 (first independent pass). Result: pass — the modular monolith still holds.** The counts below grew as specs 0008/0009 built on it; everything green.
> - **Build:** `dotnet build Quiztin.sln -c Release` → **0 errors** (38 pre-existing nullable-reference warnings in the Assessment persistence layer, none new). Solution is the host + two modules + three test projects.
> - **Tests:** `dotnet test Quiztin.sln -c Release` → **142 pass, 0 failed** (Identity 36, Api 11, Assessment 95 — the last 28s of it the Testcontainers real-Postgres integration tests, which migrate a fresh `quiz` schema from scratch every run, so "fresh-Postgres migrate works" is proven there). `cd frontend && npm run build` → clean (prerender 27315 bytes).
> - **Runtime, fresh boot:** booted the single host in Development against a **brand-new** database `quiztin_v7` (`RUN_MIGRATIONS_ON_STARTUP=true`). The log shows migrate → seed → `Now listening`, with `Seeded the core loop: classroom …3, quiz …4, student …2 enrolled`. Introspecting the fresh DB: `\dn` shows **`identity` and `quiz`**; the FK that used to fail (`FK_Profiles_AuthUsers_UserId`, `identity.Profiles → identity.AuthUsers`) is **present**; the seeded `teacher@quiztin.dev` / `student@quiztin.dev` exist as `AuthUsers`; and the loop is populated — the student is enrolled in "Seed Classroom (Dev)", which has a published quiz "Networking Basics (Dev)" with 3 questions. Torn down after (host stopped, `quiztin_v7` dropped).
> - **FK-bug fix** proven structurally (the FK exists + registration creates the `AuthUsers` row, so a profile insert satisfies it) and by the Identity controller/strategy tests (`UpdateProfile_ValidRequest_ReturnsOk`, etc.). The live register→profile→login **curl** flow from the original run below was **not** re-driven — it authors auth credentials, which this credential-free pass avoids; those exact paths are covered by the passing Identity + Assessment integration tests. Spec stays **Accepted**.

---


## Build + tests
- `dotnet build Quiztin.sln -c Release` → 0 errors. Solution is the host + two module
  projects + two test projects (down from 24).
- `dotnet test Quiztin.sln -c Release` → 62 pass (Identity 36 = the old Auth 27 + User 9;
  Assessment 26, including the Testcontainers persistence test that migrates the fresh
  `quiz`-schema `InitialCreate`).
- `cd frontend && npm run build` → tsc + vite build clean.

## Runtime (proves the loop + the bug fix)
Booted the single host in Development against a fresh Postgres (`Database=quiztin`,
`RUN_MIGRATIONS_ON_STARTUP=true`). Results:
- The host migrated both schemas: `\dn` shows `identity` and `quiz`.
- The seeders ran (demo teacher/student as real users; classroom, quiz, enrolment).
- **The bug:** `POST /api/auth/register` (fresh user) → token; then
  `PUT /api/profile` → **HTTP 200** (previously 500), and `GET /api/profile` returned the
  saved `displayName`/`academicLevel`. The FK gap is gone: registration created the
  `identity.AuthUsers` row, so the `identity.Profiles → identity.AuthUsers` FK was satisfied.
- **The loop is reachable:** `POST /api/auth/login` as the seeded student succeeded, and
  `GET /api/quizzes/available` → 200 (the enrolment-scoped seeded quiz).

## Full container path (optional, matches production)
`docker compose down -v && docker compose up --build` brings up `postgres` + `app`; the app
migrates and seeds on startup and serves the SPA at `/` and the API under `/api`.
