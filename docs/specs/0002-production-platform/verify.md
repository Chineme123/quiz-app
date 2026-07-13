# 0002. Verification

How to prove this spec is satisfied. Each phase below names the acceptance criteria from [index.md](index.md) that it proves. Everything runs against real infrastructure (local Docker, real CI, a real Railway deploy); there is no mock layer.

## Phase 1: docs reconciled

- Search the repo for the stale platform claims and confirm only archival mentions remain:
  ```bash
  git grep -nE "UseSqlServer|net8\.0|Codespace" -- ':!quiz-trash' ':!docs/project-environment-and-architecture.md'
  ```
  Any hit left should be a locked decision rationale (`foundation.md` §7 #13) or a clearly labelled archival banner, never a live "this is how it works" line. **AC-13.**
- Open `README.md`: it has a Getting started section a newcomer can follow (Docker Desktop, .NET 10 SDK, Node 20, `cp .env.example .env`, `docker compose up`, the web app dev command, the gateway origin, how to run migrations and tests). **AC-13.**

## Phase 2: local dev on Docker

```bash
cp .env.example .env
docker compose up -d
docker compose ps
```
- Postgres reports healthy first, then the app services start (they wait on the health check). **AC-1.**
- `docker compose ps` shows every service up, no port already in use error. **AC-1.**
- The build context is small: no `node_modules`, `bin`, or `obj` copied in (the new `/.dockerignore`). **AC-1.**
- Run the web app on the host and confirm hot reload plus the single proxy target:
  ```bash
  cd frontend && npm ci && npm run dev
  ```
  Editing a component hot reloads; a `/api/*` call reaches the gateway (one `GATEWAY_ORIGIN`, not three service ports). **AC-1.**

## Phase 3: the gateway

- With the stack up, call the gateway origin and confirm routing to the right service (specific prefixes, never an `/api` catch all):
  ```bash
  curl -i <gateway-origin>/api/auth/refresh    # reaches AuthService
  curl -i <gateway-origin>/api/profile         # reaches UserService (401 or 404, not a gateway 404)
  curl -i <gateway-origin>/api/quizzes         # reaches QuizService
  ```
  Each returns the service's own response, proving the route mapped. **AC-2.**
- Load the gateway origin in a browser: the web app is served, and a deep link (for example `/profile`) still loads the app (the `index.html` fallback), not a 404. **AC-2.**
- Register through the app, then reload: the `quiztin_rt` cookie (path `/api/auth`) is set on the gateway origin and the reload stays signed in, proving the cookie round trips through the gateway. **AC-2.**
- Confirm the gateway forwards rather than validates: a request with a bad token still reaches the service and the service returns 401 (the gateway did not reject it first). **AC-3.**
- `curl <gateway-origin>/health` and each service `/health` answer 200. **AC-4** (health part).

## Phase 4: the images

```bash
docker build -f src/Gateway/Dockerfile -t quiztin-gateway .
docker run --rm -p 8080:8080 quiztin-gateway   # with the services + DB reachable
```
- The build runs the web app build and copies `dist` into the gateway `wwwroot`; the running image serves the app at `/` and forwards `/api`. **AC-4.**
- `/health` on the running image answers 200. **AC-4.**

## Phase 5: CI

Open a pull request and confirm all four checks run and must pass:
- **backend**: builds, restores the dotnet tools, runs the tests against a Postgres service container, runs the migration drift check, collects coverage. **AC-5.**
- **frontend**: `npm ci`, lint, `tsc --noEmit`, vitest, build. **AC-5.**
- **codeql**: analyses C# and TypeScript. **AC-5.**
- **commitlint**: checks the PR commit messages against Conventional Commits. **AC-5.**
- Push a commit with a bad message: `commitlint` fails. Make a model change with no migration: the backend drift check fails. **AC-6.**

## Phase 6: deploy to Railway

Out of band first (developer): create the Railway project and managed Postgres; create `authdb`, `userdb`, `quizdb`, `resultdb` on the one instance; add `RAILWAY_TOKEN` as a repository secret; set the per service environment variables (`JwtSettings__Secret` identical across Auth, User, Quiz; per service `ConnectionStrings__DefaultConnection` differing only by `Database`; `AuthTokens__Cookie__Secure=true`; `ASPNETCORE_ENVIRONMENT=Production`; `RUN_MIGRATIONS_ON_STARTUP=true`).

Then:
- Merge a PR to `main`: the deploy workflow runs only after the CI jobs pass (check the run's `needs`), then deploys the gateway plus Auth, User, Quiz via the Railway CLI. **AC-7.**
- Read a service's boot logs: it applied its migrations at startup (because `RUN_MIGRATIONS_ON_STARTUP=true`) and did not seed demo data (Production). **AC-8.**
- Confirm the four databases live on one instance and the connection strings differ only by `Database`. **AC-9.**
- Smoke test on the public gateway URL: register, reload (the refresh call succeeds, the cookie is `Secure=true`), create a classroom and a quiz, take the quiz, see a score. **AC-10.**
- Rollback drill: `railway redeploy` the previous image and confirm the service comes back. (Documents the rollback path in the Migration plan.)

## Phase 7: governance

```bash
gh api repos/Chineme123/quiz-app/branches/main/protection --jq '{checks: .required_status_checks.contexts, prs: .required_pull_request_reviews, admins: .enforce_admins.enabled, force: .allow_force_pushes.enabled}'
```
- The required checks list the Phase 5 job names; a required PR object is present; `enforce_admins` is true; force pushes are off. **AC-11.**
- Try a direct push to `main`: GitHub rejects it. Open a PR: it cannot merge until the checks are green. **AC-11.**
- Dependabot opens update PRs; the PR template renders on a new PR; CODEOWNERS is recognised. **AC-12.**

## Phase 8: context reconciled

- `docs/specs/0002-production-platform/` exists and is cited by `foundation.md` (new §7 entries), `architecture.md` (gateway marked built, lines 107 and 108 resolved), and `library-docs.md` (YARP, Railway CLI, CodeQL added). **AC-13.**
- A `progress-log.md` entry exists per phase. **AC-13.**

## What this does not cover

CI does not itself run the live Railway smoke test; that is the manual Phase 6 step above. The deploy workflow proves the pipeline runs and the app boots; the end to end user flow is checked by hand on the public URL until an automated post deploy smoke test is added (a later follow up).
