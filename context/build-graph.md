# Quiztin — Build Graph

> A dependency map, not a plan. What depends on what. For *why* anything is built, see `foundation.md`; for what already exists, see `progress-log.md`.

## How to read this file
This is a map of *what-requires-what* — **NOT a timeline, a build plan, or a prescribed order**. The developer builds breadth-first with Claude (foundation §0), picking their own path; this file just says what a node genuinely needs before it can work. **Hard requirement** = cannot build/run without it. **Soft benefit** = easier or nicer with it, possible without.

## Layer 0 — foundational prerequisites (nearly everything needs these)
These are the must-fixes from `foundation.md` §8. Until they're in, features built on top are building on sand.
- **`global.json` + framework pin** to .NET 10; reconcile every `.csproj` (fix the duplicated-header one).
- **PostgreSQL up** via `docker-compose` (swap the `mssql` image for `postgres`), connection strings in config/secrets.
- **Regenerated EF migration** against Postgres, including the attempt-side tables (QuizAttempt/QuizAnswer/Enrollment/ProcessedCommand). *Nothing that touches the DB runs without this — step one.*
- **Canonical identity plumbing**: `Guid UserId` from the JWT `NameIdentifier` claim, everywhere; remove hardcoded IDs.
- **Minimal AuthService** issuing HS256 JWTs; shared issuer/audience/key from secrets.
- **API gateway (YARP) + CORS** as the single frontend origin.

## The keystone unlock
The **create→take→results→feedback loop**. It's unblocked in this order: **(1)** the Postgres migration → **(2)** the redesigned scoring contract (a strategy that can see correct answers) → **(3)** ResultService's graded-event projection. Once those three exist, the loop closes and everything else is elaboration.

## Dependencies (X needs Y)
- **UC2 Create Classroom** — hard: identity/auth (teacher principal), Classroom model *(built)*. soft: gateway.
- **UC3 Join Classroom** — hard: UC2, Enrollment model *(built)*, identity. → unlocks **FR7** enforcement.
- **UC6 Create Quiz (manual)** — hard: Quiz+Question model *(built)*, UC2 (a quiz needs a classroom), identity. *(mostly built)*
- **UC6 AI generation (real)** — hard: Claude client + **fallback**; soft: UC6 manual path *(built)*.
- **UC8 Take Quiz** — hard: migration, Quiz/Question *(built)*, UC3 enrolment (FR7), the **scoring contract redesign**; soft: real auth (testable with a seeded identity).
- **UC8 AI feedback** — hard: Claude client + fallback, **scoring redesign** (feedback needs question + correct answer + student answer per `security.md` §2).
- **UC8 attempt rules** (`Abandoned` triggers 1–4, one-active-attempt) — hard: QuizAttempt state machine *(built)*, `Quiz.CanStart`/attempt-limit *(built)*.
- **UC9 View My Results** — hard: **ResultService** + graded-event projection + attempts existing (UC8).
- **UC10 Classroom Results** — hard: UC9 read-side, classroom ownership (UC2). soft: UC9 first.
- **UC14 Profile** *(built)* — hard: identity; must-fix: provision the `Users` row (FK gap), partial-update, add resiliency.
- **Frontend screens** — hard: the relevant service API + the design tokens *(export landed, `ui-*.md` generated)*. soft: the YARP gateway. Same-origin in **dev** comes from the **Vite proxy**, not the gateway (foundation §7 #27, spec 0001), so the gateway is a production concern and does **not** block SPA development. The auth screens + Manage Profile also need AuthService `refresh`/`logout` *(built, PR #23)*.

## Buildable from a cold start (no prerequisites)
`global.json`; the `docker-compose` Postgres service; the Claude client wrapper **+ its deterministic fallback** (behind the strategy interface — can be built and tested before the real API key); the YARP gateway skeleton; HTTP DTO contracts; test fixtures for the state machine and scoring.

## External dependencies with lead time ⏳
- **Claude API key** — provision before the AI paths can run live (the fallback lets you build without it).
- **Railway project + managed Postgres** — provision before deploy; local Docker Postgres unblocks all dev.
- **Claude Design export** — ⏳ **blocks the UI trio** (`ui-tokens`/`ui-rules`/`ui-registry`) and therefore the SPA's final styling. Start it early; the SPA's *logic* can be built against placeholder styling.

## The one genuine tension
**Identity/auth vs. feature velocity.** Every classroom/quiz/attempt feature wants a real `Guid UserId` from a JWT, but auth is unbuilt and the code currently hardcodes IDs. Two honest paths: **(a)** build the minimal AuthService + claims plumbing first — identity is real from the start, but visible features wait; **(b)** stub a dev identity/token and build features in parallel, wiring real auth later — faster visible progress, but you re-touch every controller when real auth lands. **Recommendation:** build the *minimal* AuthService + claims extraction early (it's small and Layer-0 anyway), so identity is real everywhere from day one — just don't gold-plate it.

## Explicitly out of scope (see `foundation.md` §8 Deferred)
NotificationService; UC7/UC11/UC12/UC13; UC15–19; practice mode (UC4/UC5); question types 4–5; full multiple-attempt history; save-draft; auto-save; background abandon-sweeper; event outbox. The `quiz-trash/` cleanup is the final step, after the system is otherwise done.
