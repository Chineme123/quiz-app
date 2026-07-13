# 0002. Rationale

The context, the options weighed, and why. The build spec itself is in [index.md](index.md).

## Context

Quiztin's application layer is in good shape: five .NET 10 services (Auth, User, Quiz built; Result and Notification are stubs), a Postgres database, and a shipped React app (spec 0001). What is missing is everything around the application: the platform.

The forces that made this urgent:

- **Continuous integration covers half the product.** The one workflow builds and tests the .NET solution and never touches the React app. A broken screen, a lint failure, or an untested change can reach `main` with a green badge that means nothing about the frontend.
- **Nothing deploys.** There is no hosting, no deploy pipeline, and no gateway. The intended production shape (a YARP gateway as the single origin, containers on Railway) is documented in `foundation.md` §7 but zero percent built.
- **The local setup drifted while the app moved on.** `docker-compose.yml` still carries host port collisions (NotificationService on 5005, which is AuthService's development port) and inconsistent secret wiring; the local development story was "no Docker, use Homebrew Postgres" only because Docker Hub was blocked on the developer's network. That block is now gone.
- **The session design constrains the platform.** Spec 0001 keeps the access token in memory and the refresh token in an `HttpOnly` cookie scoped to `/api/auth`. That only works if the web app and the API are one origin. There is no CORS anywhere in the backend, on purpose. So production cannot serve the app from a different host than the API without reopening a security decision.
- **Two build time questions were left open** in `architecture.md`: whether the gateway or each service validates the JWT (line 107), and how the per service databases sit on one managed instance (line 108). Both must be settled before deploy.

The consequence of not deciding: the app stays a local only artifact, the frontend stays unchecked, and the eventual "let us deploy" moment becomes a large, risky, all at once effort instead of an incremental one.

A note on scope that shaped this: the microservice split itself is a coursework constraint from the Agile Unified Methodology, not a choice this spec revisits. This spec takes the services as given and builds the platform that runs them.

## Options considered

Each load bearing fork below was weighed with the developer during planning; the chosen answers are recorded in [index.md](index.md) under Decision.

### Fork 1: how the web app is served in production

- **Gateway serves the app same origin (chosen).** The app is baked into the gateway image and served from `wwwroot`; the gateway also forwards `/api`. Pros: one origin, so the refresh cookie design keeps working with no change; one deployable; no CORS. Cons: the gateway image rebuilds when the app changes.
- **Separate static host (nginx, a static platform) with the gateway proxying to it.** Pros: the app deploys independently of the gateway. Cons: either it is a different origin (which breaks the `HttpOnly` cookie and forces CORS, reopening a settled security decision), or the gateway has to proxy `/` to it anyway, which is more hops and services for no gain.

### Fork 2: who validates the JWT (resolves `architecture.md:107`)

- **Services stay authoritative, the gateway forwards (chosen).** Pros: defence in depth; each service still refuses an unauthenticated call, which matters around student data; the gateway stays a thin, stateless router. Cons: the token is validated more than once per request (cheap; the signature check is fast).
- **Gateway validates, services trust it.** Pros: one validation point. Cons: a single point whose bypass exposes every service; the services would have to trust network position, which is fragile. Deferred to a later "both layers" hardening, not v1.

### Fork 3: continuous delivery mechanism

- **GitHub Actions plus the Railway CLI, gated on CI (chosen).** Pros: deploys run only after the checks pass, so a red build never ships; migrations can run in the same gated job; one place to sequence things. Cons: a little more YAML to own than the alternative.
- **Railway native GitHub auto deploy.** Pros: least setup. Cons: it builds and ships on push independently of the GitHub checks, so it can deploy a commit whose CI is failing. That defeats the point of the checks.

### Fork 4: database topology on Railway (resolves `architecture.md:108`)

- **One managed Postgres, four databases (chosen).** Pros: mirrors the local `docker/postgres-init.sql` layout exactly; one instance to fund and operate; per service isolation by database name. Cons: shared instance resources; a heavy service could affect a neighbour (acceptable at this scale).
- **One managed Postgres plugin per service.** Pros: full isolation. Cons: four or five managed databases to pay for and operate, which is real money and effort for a solo student project with no isolation requirement.

### Fork 5: local development after Docker Hub was unblocked

- **Move local dev onto `docker compose` (chosen).** Pros: one command up; the local database and services match production images; the drifted compose file gets fixed rather than left to rot. Cons: needs Docker Desktop running.
- **Keep Homebrew Postgres and hand run services.** Pros: no Docker dependency. Cons: keeps five hand starts and a database that does not match the deployed image; leaves the compose file drifting.

The web app dev server stays on the host (Vite, for hot reload), proxying to the containerised gateway. Containerising the app is a production artifact only, never the active dev loop.

## Rationale

The session design is the load bearing constraint, and it decides Fork 1 almost on its own: an in memory access token with an `HttpOnly` refresh cookie is only safe when the app and API share an origin, so the gateway serving the app same origin is not a preference, it is what the existing security posture requires. Serving the app from a separate host would reopen a decision spec 0001 already closed.

Fork 2 follows from where the sensitive data is. Student records sit behind the services, so each service refusing an unauthenticated call (defence in depth) is worth the negligible cost of validating a signature twice. A gateway only model would make network position the security boundary, which is the fragile pattern this project's `security.md` avoids.

Fork 3 is decided by the whole reason CI exists. If the deploy can outrun the checks, the checks are theatre. Gating the deploy on the CI jobs, via Actions rather than Railway's independent auto build, is what makes "green means shippable" true.

Forks 4 and 5 are shaped by the developer being solo on a student budget. One managed database with four logical databases matches what the app already expects locally and costs one instance, not four. Moving local dev onto Docker is now free (the network block is gone) and pays back by killing the compose drift and giving image parity with production.

The strangler sequencing (build the platform beside the running app, cut over per phase) is the standard safe path for adding infrastructure to a working system, and it lets every phase ship as its own reversible PR rather than one large risky change.

## References

**Project sources** (verifiable in this repo)

- `context/foundation.md` §7 #16 (YARP gateway as the single origin), #22 (Docker to Railway hosting), #27 (same origin, no CORS): the decisions this spec executes.
- `docs/specs/0001-frontend-foundation/` (the in memory token plus rotating `HttpOnly` refresh cookie session design): the constraint behind Fork 1 and Fork 2.
- `context/security.md` (never make network position the security boundary; defence in depth around student data): behind Fork 2.
- `context/architecture.md` lines 107 and 108: the two open questions this spec resolves.
- The approved implementation plan for this work (Context, phases, verification), captured during planning.

**Practices and standards**

- Strangler pattern for adding to a live system (build beside, cut over incrementally, retire nothing that works).
- Gate deploys on the same checks that gate merges (a deploy that can outrun CI makes CI meaningless).
- Migrate on startup with additive migrations and a retry, plus an idempotent script fallback, for zero touch schema rollout on a single instance per service.
