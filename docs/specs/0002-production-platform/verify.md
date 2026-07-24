# 0002. Verification

How to prove this spec is satisfied. Everything runs against real infrastructure (local Docker, real CI, a real Railway deploy); there is no mock layer. Each phase names the acceptance criteria from [index.md](index.md) it proves.

> **Rewritten 2026-07-24 for the single-host architecture.** The original steps targeted the five-microservice topology this spec built: a YARP gateway at `src/Gateway/`, separate `AuthService` / `UserService` / `QuizService`, and four databases. Spec 0007 folded the services and the gateway into **one `Quiztin.Api` host** on port **8080**, and the four databases into **one `quiztin` database** with `identity` and `quiz` schemas. The platform this spec delivered (Docker, CI, Railway CD, governance) is unchanged — the checks below are the same in substance; the routing and per-service steps now target the one host. The old per-service script is in git history.

> **Verified 2026-07-24 (first independent pass). Result: MIXED — the platform is sound, but the live deployment is broken.**
> - **Pass, by inspection:**
>   - **Phase 1 (AC-13):** the `UseSqlServer`/`net8.0`/`Codespace` hits are confined to archival-bannered files (`scaffold.sh`) or the AC text itself, not any live "this is how it works" line; `README.md` has a Getting started. **Phase 8 (AC-13):** `foundation.md`, `architecture.md`, and `library-docs.md` all cite 0002; `progress-log.md` has per-phase entries.
>   - **Phase 2 (AC-1):** `.dockerignore` present; `docker-compose.yml` gates the app on `postgres` `service_healthy` and exposes one origin (`:8080`).
>   - **Phase 3 (AC-2/AC-3):** `src/Quiztin.Api/Program.cs` routes `/api/*` to the module controllers in-process (`MapControllers`), serves the SPA (`MapGet("/")` + `MapFallbackToFile("index.html")`), keeps the modules auth-authoritative (`UseAuthentication`/`UseAuthorization`), and answers `/health`.
>   - **Phase 4 (AC-4):** `src/Quiztin.Api/Dockerfile` is multi-stage (`node:20` builds the SPA → `sdk:10.0` builds the host → `aspnet:10.0` final with `COPY --from=spa … ./wwwroot`), so one image serves the app and the API.
>   - **Phase 5 (AC-5/AC-6):** the four required checks exist and are **green in practice** on `main` (`build-and-test` incl. the migration drift check + coverage, `frontend`, `codeql`, `commitlint`).
>   - **Phase 7 (AC-11/AC-12):** branch protection (via `gh api`) has `enforce_admins: true`, force-pushes off, a PR required, and required checks matching the CI jobs; `CODEOWNERS`, a PR template, and `dependabot.yml` all present.
>   - **AC-8** (migrate-on-startup) is wired in `Program.cs` and was seen working in the 0007 fresh-boot verify; **AC-9** is now one `quiztin` database (schema-per-module).
> - **FAIL, live (Phase 6, AC-7 and AC-10):** the **Deploy workflow has failed on every merge to `main` since ~2026-07-20** (last green: `b1f1565`, 07-20; the last three runs all `failure`), and the public URL this spec shipped — `https://gateway-production-02f8.up.railway.app` — now returns **404 "Application not found"**. `railway up --service quiztin --ci` fails in ~9s. Root cause: spec 0007 (2026-07-18) folded the five services into one `quiztin` host and updated the **repo** side to match (`deploy.yml` → `--service quiztin`, `railway.json` → `src/Quiztin.Api/Dockerfile` — both correct and internally consistent), but the **Railway project was never reconciled**: there is no working `quiztin` service, and the old public `gateway-production` service is gone. Deploy is not a required status check, so this did not block merges — it shipped nothing, silently, for two weeks.
> - **Not run:** the live register/reload smoke test on the public URL (AC-10) is moot while the deployment is down, and needs credentials besides.
> - **Follow-up (needs Railway dashboard access — cannot be fixed from the repo):** stand up a `quiztin` service in the Railway project pointed at `src/Quiztin.Api/Dockerfile`, set its env (`ConnectionStrings__DefaultConnection` → the managed `quiztin` DB, `JwtSettings__Secret`, `AuthTokens__Cookie__Secure=true`, `ASPNETCORE_ENVIRONMENT=Production`, `RUN_MIGRATIONS_ON_STARTUP=true`), confirm `RAILWAY_TOKEN` scopes to that project, redeploy, then update the public URL in `README.md`. The spec stays **Accepted** (it was correctly delivered and live through 2026-07-20); AC-7/AC-10 are flagged **regressed** until the Railway side is reconciled.

## Phase 1: docs reconciled

- Confirm only archival mentions of the retired stack remain:
  ```bash
  git grep -nE "UseSqlServer|net8\.0|Codespace" -- ':!docs/project-environment-and-architecture.md'
  ```
  Any hit should be a locked decision rationale (`foundation.md` §7 #13) or a labelled archival banner, never a live "this is how it works" line. **AC-13.**
- `README.md` has a Getting started a newcomer can follow (Docker Desktop, .NET 10 SDK, Node, `cp .env.example .env`, `docker compose up`, the web dev command, the origin, how to run migrations and tests). **AC-13.**

## Phase 2: local dev on Docker

```bash
cp .env.example .env
docker compose up -d
docker compose ps
```
- Postgres reports healthy first, then the app starts (it waits on the health check). **AC-1.**
- `docker compose ps` shows Postgres and the one app up, no port collision. **AC-1.**
- Small build context: no `node_modules`, `bin`, or `obj` copied in (`/.dockerignore`). **AC-1.**
- Run the web app on the host and confirm hot reload plus the single proxy target:
  ```bash
  cd frontend && npm ci && npm run dev
  ```
  Editing a component hot reloads; a `/api/*` call reaches the host (one origin, not several service ports). **AC-1.**

## Phase 3: the single origin (was "the gateway")

The host now plays the gateway's role: it serves the SPA and routes `/api` to the modules in-process.

- With the stack up, confirm each module answers on its own prefix, never an `/api` catch-all:
  ```bash
  curl -i localhost:8080/api/auth/refresh    # Identity module (401 without a cookie)
  curl -i localhost:8080/api/profile         # Identity module (401 or 404, not a routing 404)
  curl -i localhost:8080/api/quizzes/available  # Assessment module (401 without a token)
  ```
  Each returns the module's own response, proving the route mapped. **AC-2.**
- Load `:8080` in a browser: the SPA is served, and a deep link (e.g. `/profile`) still loads the app (the `index.html` fallback), not a 404. **AC-2.**
- Register through the app, then reload: the `quiztin_rt` cookie (path `/api/auth`) is set on the origin and the reload stays signed in. **AC-2.**
- Confirm the host forwards auth to the module rather than terminating it: a request with a bad token still reaches the endpoint and gets the module's 401. **AC-3.**
- `curl localhost:8080/health` answers 200. **AC-4** (health part).

## Phase 4: the image

```bash
docker build -t quiztin-app .
docker run --rm -p 8080:8080 quiztin-app   # with the DB reachable
```
- The multi-stage build compiles the SPA and copies `dist` into the host's `wwwroot`; the running image serves the app at `/` and routes `/api`. **AC-4.**
- `/health` on the running image answers 200. **AC-4.**

## Phase 5: CI

Open a pull request and confirm all four checks run and must pass:
- **backend**: builds, restores the dotnet tools, runs `dotnet test` against a Postgres service container, runs the migration drift check, collects coverage. **AC-5.**
- **frontend**: `npm ci`, lint, `tsc --noEmit`, vitest, build. **AC-5.**
- **codeql**: analyses C# and TypeScript. **AC-5.**
- **commitlint**: checks the PR commits against Conventional Commits. **AC-5.**
- Push a bad commit message: `commitlint` fails. Make a model change with no migration: the backend drift check fails. **AC-6.**

## Phase 6: deploy to Railway

Out of band (developer): the Railway project and managed Postgres exist; the `quiztin` database is created (with the `identity` and `quiz` schemas applied by startup migrations); `RAILWAY_TOKEN` is a repo secret; the app's environment is set (`JwtSettings__Secret`; `ConnectionStrings__DefaultConnection` → the `quiztin` DB; `AuthTokens__Cookie__Secure=true`; `ASPNETCORE_ENVIRONMENT=Production`; `RUN_MIGRATIONS_ON_STARTUP=true`).

Then:
- Merge a PR to `main`: the deploy workflow runs only after the CI jobs pass (check the run's `needs`), then deploys the one app via the Railway CLI. **AC-7.**
- Read the boot logs: it applied migrations at startup (`RUN_MIGRATIONS_ON_STARTUP=true`) and did not seed demo data (Production). **AC-8.**
- Confirm the app runs on one managed Postgres instance, one `quiztin` database, schema-per-module. **AC-9.**
- Smoke test on the public URL: register, reload (the refresh succeeds, the cookie is `Secure=true`), create a classroom and a quiz, take the quiz, see a score. **AC-10.**
- Rollback drill: `railway redeploy` the previous image and confirm the app comes back.

## Phase 7: governance

```bash
gh api repos/Chineme123/quiz-app/branches/main/protection --jq '{checks: .required_status_checks.contexts, prs: .required_pull_request_reviews, admins: .enforce_admins.enabled, force: .allow_force_pushes.enabled}'
```
- The required checks list the Phase 5 job names; a required PR object is present; `enforce_admins` is true; force pushes are off. **AC-11.**
- Try a direct push to `main`: rejected. Open a PR: it cannot merge until the checks are green. **AC-11.**
- Dependabot opens update PRs; the PR template renders; CODEOWNERS is recognised. **AC-12.**

## Phase 8: context reconciled

- `docs/specs/0002-production-platform/` exists and is cited by `foundation.md` (§7 entries), `architecture.md`, and `library-docs.md` (YARP, Railway CLI, CodeQL). **AC-13.**
- A `progress-log.md` entry exists per phase. **AC-13.**

## What this does not cover

CI does not itself run the live Railway smoke test; that is the manual Phase 6 step. The deploy workflow proves the pipeline runs and the app boots; the end-to-end user flow is checked by hand on the public URL until an automated post-deploy smoke test is added (a follow-up).

Note (post-0007): AC-2/AC-7/AC-9 were written for the four-service, four-database topology. They are proven above against the single host and the one `quiztin` database, which is what the substitution in the index banner means in practice — the routing, the CI-gated deploy, and the migrate-on-startup all still hold; only the process and database counts changed.
