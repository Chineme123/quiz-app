# 0007. Modular monolith — verification

Run from the repo root. The local .NET 10 SDK lives at `~/.dotnet`
(`export DOTNET_ROOT="$HOME/.dotnet"; export PATH="$HOME/.dotnet:$PATH"`).

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
