# 0002. Production platform: gateway, containerised local dev, CI, and deploys to Railway

**Date**: 2026-07-13
**Status**: Proposed

## Summary

Quiztin has a healthy application (backend services plus a shipped React app) but no real platform around it: continuous integration (the automated check on every change) covers only the .NET code, nothing deploys anywhere, and the container setup has drifted. This spec settles that platform. It adds one front door (a YARP gateway that both serves the web app and forwards `/api` calls to the right service), moves local development onto `docker compose` now that Docker works again on the developer's machine, widens the automated checks to cover the whole product, and ships every merge to Railway (a managed hosting service) once the checks pass. It also hardens the GitHub project settings so the rules are enforced by the server, not just by local hooks.

## Context

Reasoning, the options weighed, and the evidence live in [rationale.md](rationale.md).

## Requirements

**User stories**

- As the developer, I want one command to bring the whole stack up locally, so I do not hand start five services and a database.
- As the developer, I want every change checked automatically (backend, frontend, and security), so a broken build or an untested screen cannot reach `main`.
- As the developer, I want a merge to `main` to deploy itself once the checks pass, so shipping is not a manual, error prone ritual.
- As a signed in user, I want the deployed app to keep me signed in across a reload exactly as it does locally, so the production session is as safe as the local one.
- As the developer, I want the repository rules enforced by GitHub itself, so a slip on one machine cannot push straight to `main`.

**Acceptance criteria**

- **AC-1**: `docker compose up` starts Postgres plus the built services; app services start only after the Postgres health check passes; no two services claim the same host port. The web app runs on the host Vite server and proxies `/api/*` to the gateway with hot reload intact.
- **AC-2**: The gateway routes `/api/auth/**` to AuthService, `/api/profile/**` to UserService, and `/api/classrooms/**`, `/api/quizzes/**`, `/api/attempts/**` to QuizService, never with an `/api` catch all. It serves the web app from `wwwroot` with a fallback to `index.html` for client routing, and the `quiztin_rt` refresh cookie (path `/api/auth`) round trips through the gateway unchanged.
- **AC-3**: Each service validates the JWT itself. The gateway forwards the `Authorization` header and cookies without validating or terminating auth.
- **AC-4**: `docker build` of the gateway image builds the web app and copies it into `wwwroot`, producing one image that serves the app and the API at one origin. The gateway and every service answer a `/health` endpoint.
- **AC-5**: A pull request to `main` runs four checks that all must pass: backend (build plus tests against a real Postgres plus a migration drift check plus coverage), frontend (install, lint, type check, vitest, build), CodeQL (C# and TypeScript), and a Conventional Commits check over the PR commits.
- **AC-6**: A model change with no matching Entity Framework migration fails the backend check.
- **AC-7**: A merge to `main` runs the deploy workflow only after the CI checks pass, and it deploys the gateway, AuthService, UserService, and QuizService to Railway using the Railway CLI.
- **AC-8**: Each deployed service applies its migrations at startup when `RUN_MIGRATIONS_ON_STARTUP=true`, separate from the demo seeder, which runs only in Development.
- **AC-9**: The four databases `authdb`, `userdb`, `quizdb`, `resultdb` live on one Railway managed Postgres instance; each service connection string differs only by the `Database` name.
- **AC-10**: In production the refresh cookie is issued with `Secure=true`, and a register, reload, refresh round trip works on the public gateway URL.
- **AC-11**: A direct push to `main` is rejected by GitHub; a PR cannot merge until the required checks pass; `enforce_admins` is on; a PR is required (with zero required approvals, since a solo developer cannot approve their own PR).
- **AC-12**: Dependabot (nuget, npm, github-actions), a CODEOWNERS file, and a PR template exist and work.
- **AC-13**: The living context docs no longer describe SQL Server, Codespaces, or "Docker blocked locally" as current; the root `README.md` has an accurate Getting started section; `foundation.md` §7 records the new decisions and cites this spec.

## Decision

**Chosen option**: Option 2: a YARP gateway as the single front door, container based local development, an expanded CI plus a CI gated GitHub Actions deploy to Railway, and governance enforced as code.

Build the platform incrementally alongside the running app (the strangler approach): stand the gateway up beside the existing Vite proxy, move local dev onto `docker compose`, widen CI, then add the deploy path, then tighten GitHub settings. Nothing in the running application changes shape; the platform is added around it.

The locked choices inside this decision:

- **Gateway serves the web app same origin.** The web app is baked into the gateway image and served from `wwwroot`; the gateway also forwards `/api`. This is required by the session design (spec 0001): the access token lives in memory and the refresh token is an `HttpOnly` cookie scoped to `/api/auth`, so the app and the API must share one origin.
- **Services stay authoritative for auth.** The gateway forwards credentials and does not validate them. This keeps defence in depth around student data (each service still refuses an unauthenticated call). This resolves the open question at `context/architecture.md:107`.
- **One Railway Postgres, four databases** (`authdb`, `userdb`, `quizdb`, `resultdb`); NotificationService is deferred. This mirrors the local `docker/postgres-init.sql` layout and resolves the open question at `context/architecture.md:108`.
- **Migrate on startup, gated by an env flag** (`RUN_MIGRATIONS_ON_STARTUP`), pulled out of the Development only seeder so production applies migrations without seeding demo data.
- **Deploy through GitHub Actions, gated on CI**, using the Railway CLI and a `RAILWAY_TOKEN` secret, rather than Railway's own auto deploy (which would build independently of the GitHub checks and could ship a red build).
- **One environment first (production).** The deploy workflow is parameterised so a staging tier can be added later without rework.

## New platform components

| Layer | Choice | Reason |
|---|---|---|
| API gateway | YARP (`Yarp.ReverseProxy`) on .NET 10 | Same runtime as the services; config driven routes; serves static files, so one process is both the app host and the `/api` front door |
| Local orchestration | `docker compose` (Postgres 17 plus service images) | Docker works on the machine again; one command up; parity with the production image build |
| CI | GitHub Actions: backend on a Postgres service container, frontend on Node 20, CodeQL, commit lint | Covers the whole product and gives the checks that branch protection can require |
| Hosting | Railway (managed Postgres plus container services) | Locked in foundation §7 #22; managed database, container deploys, solo budget friendly |
| CD | GitHub Actions plus Railway CLI, gated on CI | Deploys only after the checks are green; one place to also run migrations before or during rollout |
| Governance | Dependabot, CODEOWNERS, PR template, branch protection via `gh api` | The local hooks become real server side rules; dependency and code scanning run automatically |

## Build plan

Ordered by the strangler approach: the gateway (the new seam) first, then the container and image work that depend on it, then the checks, then the deploy, with the docs and governance running alongside. Full detail and verification live in [verify.md](verify.md).

1. **Reconcile the stale platform docs and write a real run doc.** Reword the living stale spots (`foundation.md` §0 dev environment, §8, §10, §11; `library-docs.md`; `build-graph.md`) to current; add archival banners to `add_docker.sh`, `scaffold.sh`, and `docs/project-environment-and-architecture.md`; add a Getting started section to `README.md`. Satisfies **AC-13**.
2. **Move local development onto Docker.** Add `/.dockerignore`; fix `docker-compose.yml` (a non colliding host port block, gate app services on the Postgres health check, unify the secret wiring, set the environment consistently); collapse the Vite proxy to one `GATEWAY_ORIGIN`. Satisfies **AC-1**.
3. **Build the YARP gateway** at `src/Gateway/`: config driven routes on specific `/api` prefixes, serve the web app from `wwwroot` with an `index.html` fallback, forward credentials with services staying authoritative, a `/health` endpoint on the gateway and each service. Register it in `QuizApp.sln` and `docker-compose.yml`. Satisfies **AC-2**, **AC-3**.
4. **Complete the Docker images.** Write the gateway's multi stage Dockerfile that builds the web app and bakes it into `wwwroot`. Service Dockerfiles are already .NET 10; the new `/.dockerignore` shrinks their build context. Satisfies **AC-4**.
5. **Expand CI.** Backend job with a Postgres service container, `dotnet tool restore`, tests against real Postgres, a migration drift check, and coverage; a frontend job (install, lint, type check, vitest, build); a CodeQL job; a Conventional Commits job. Finalise the job names before Phase 7. Satisfies **AC-5**, **AC-6**.
6. **Deploy to Railway.** A `deploy.yml` that runs on push to `main`, needs the CI jobs, and deploys the gateway plus Auth, User, Quiz via the Railway CLI. Pull `MigrateAsync` out of the Development only seeder into a startup hook gated by `RUN_MIGRATIONS_ON_STARTUP`. Create the four databases on one Railway Postgres. Satisfies **AC-7**, **AC-8**, **AC-9**, **AC-10**.
7. **Harden GitHub governance.** Add `dependabot.yml`, `CODEOWNERS`, and a PR template; refine branch protection via `gh api` (require a PR with zero approvals, turn on `enforce_admins`, update the required check names to the Phase 5 jobs once they have reported once). Satisfies **AC-11**, **AC-12**.
8. **Reconcile the context system.** Update `foundation.md`, `architecture.md`, `library-docs.md` to cite this spec and mark the gateway built and the two open questions resolved; add a progress log entry per phase. Satisfies **AC-13**.

## Migration plan

**Strategy**: strangler (build the new platform beside the running app, cut over incrementally, retire nothing that still works).

**Phases**:
1. Docs and local Docker (Phases 1 to 2): no runtime behaviour changes; the app still runs, now via `docker compose`.
2. Gateway beside the Vite proxy (Phases 3 to 4): the gateway becomes the local single origin; the Vite proxy now points at the gateway instead of three service ports. The production path does not exist yet, so there is nothing to break.
3. CI then CD (Phases 5 to 6): checks widen first (safe, only adds gates), then the deploy path turns on. The first real deploy is the cutover to production.
4. Governance (Phase 7): tighten the server side rules only after the new checks report, so a required check that never posts cannot deadlock merges.

**Rollback**: each phase is its own PR and reverts cleanly. A bad production deploy rolls back with `railway redeploy` to the previous image; the database is unaffected because migrations are additive.

**Risks**: a required status check pinned before it has posted would block all merges (mitigated by ordering Phase 7 after Phase 5 has run once); a startup migration that fails would keep a service down (mitigated by Npgsql retry, one instance per service so no migration race, and an idempotent SQL script published as a CI artifact for manual apply).

## Consequences

**Positive**

- One command local dev, and one front door that is the same shape locally and in production, so the move to Railway is configuration, not a rewrite.
- Every change is checked across the whole product before it can merge, and every merge deploys itself, so shipping stops being manual.
- The refresh cookie design keeps working unchanged in production because the gateway preserves the single origin.
- The repository rules are enforced by GitHub, not by hooks a contributor might not have installed.

**Negative and tradeoffs**

- Running a gateway plus four services plus a managed database is more moving parts than a single deployable would be. The microservice split is a pre existing coursework constraint, not this spec's choice, but this spec does take on operating it.
- A CI gated deploy adds latency between merge and live (the checks must finish first). That is the cost of never shipping a red build.
- Migrate on startup couples a deploy to a schema change; a bad migration keeps the service down until fixed. The idempotent SQL fallback and additive migrations bound the damage.
- Railway is a managed dependency with its own billing and limits; the project now has an external host to keep funded and watch.

**Neutral**

- NotificationService and ResultService stay out of the deploy for now (Notification deferred, Result deploys when its read side lands). The compose file and topology are written so adding them later is a small change.
- The gateway adds `Yarp.ReverseProxy` and the platform adds Railway CLI and CodeQL to the tool surface, recorded in `library-docs.md` in Phase 8.

## Follow-up

- [ ] Add a staging environment on Railway once production is proven; the deploy workflow is already parameterised for it.
- [ ] Add gateway level JWT validation as a fail fast hardening ("both" layers), after the services authoritative model is running.
- [ ] Deploy ResultService once UC9 and UC10 (the results views) exist.
- [ ] Attach a custom domain to the gateway and decide the production CORS posture if any third party origin is ever needed (none today).

## Rationale

The problem framing, the options weighed (gateway topology, CD mechanism, migration strategy, database topology, local dev on Docker), and the evidence live in [rationale.md](rationale.md).
