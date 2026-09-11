# Quiztin — Architecture

> How the pieces fit. For *why* any choice was made, see `foundation.md` (cited as §7 #N) — it wins if this file ever disagrees. Coding conventions live in `code-standards.md`; the stack details live in `library-docs.md`.
> **⚠️ Superseded by [spec 0007](../docs/specs/0007-modular-monolith/index.md) (2026-07-18):** the multi-service shape and the YARP gateway described below are now a **modular monolith** — one `Quiztin.Api` host serving `/api` + the SPA, two module projects (`Quiztin.Modules.Identity`, `Quiztin.Modules.Assessment`), one `quiztin` database with a schema per module. No gateway, no per-service databases. **`ResultService` is not a separate service** — UC9/UC10 results are the `Assessment` module's read/reporting side, aggregated on read (no projection, no `resultdb`). Read the shape here through that lens until this file is fully swept.

## Shape

```
                         ┌─────────────────────────┐
                         │  React + Vite SPA        │   frontend/  (§7 #5)
                         │  (teacher + student UI)  │
                         └───────────┬─────────────┘
                                     │  one origin, HTTPS + JWT
                         ┌───────────▼─────────────┐
                         │  YARP API Gateway        │   src/Gateway/  (§7 #16)
                         │  routing · CORS · auth   │
                         └─┬───────┬───────┬──────┬─┘
              ┌────────────┘   ┌───┘       └───┐  └──────────────┐
        ┌─────▼─────┐   ┌──────▼─────┐   ┌─────▼──────┐   ┌──────▼──────────┐
        │AuthService│   │UserService │   │QuizService │   │ResultService    │
        │issues JWT │   │profiles    │   │quizzes,    │   │results/reporting│
        │(§7 #15)   │   │(UC14)      │   │attempts,   │   │(UC9/UC10) reads │
        │           │   │            │   │grading     │   │via projection   │
        └─────┬─────┘   └──────┬─────┘   └─────┬──────┘   └──────┬──────────┘
              │                │               │ graded event   │
              │                │               └───────────────►│ (§7 #8)
        ┌─────▼────────────────▼───────────────▼────────────────▼─────┐
        │           PostgreSQL — one instance, database per service    │  (§7 #10)
        └──────────────────────────────────────────────────────────────┘

    NotificationService — scaffold, deferred (not in v1).   External: Anthropic Claude API (§7 #6, security.md).
```

The SPA talks to the single `Quiztin.Api` host. Grading happens in the `Assessment` module at submission; results (UC9/UC10) are aggregated on read from that same module's attempt data — no projection, no separate service (spec 0007).

## Stack

| Layer | Choice | Notes |
|---|---|---|
| Frontend | React + Vite (SPA) | §7 #5; own toolchain, not in `QuizApp.sln` |
| Gateway | YARP reverse proxy (.NET), serves the SPA + routes `/api` | §7 #16/#30; single origin, **no CORS** (§7 #27); services stay JWT-authoritative |
| Backend | ASP.NET Core Web API × 5 | Clean Architecture per service |
| Language / runtime | C# / **.NET 10** | §7 #13; pinned via `global.json` |
| ORM | EF Core 10 + **Npgsql** | §7 #18; TPH, `jsonb`, `xmin` concurrency, retry-on-failure |
| Database | **PostgreSQL** | §7 #10; database-per-service, one instance |
| Auth | JWT (HS256), self-issued + rotating refresh cookie | §7 #15/#26; AuthService mints, all validate (issuer/audience `quiztin`); in-memory access token + `HttpOnly` refresh cookie (built PR #23) |
| AI | Anthropic Claude API | §7 #6; generation + feedback, with deterministic fallback |
| Validation / mapping | Manual (no FluentValidation/AutoMapper) | §7 #21 |
| Hosting | Docker → Railway (+ managed Postgres) | §7 #22; **live** (spec 0002): gateway public, 3 services private, 4 DBs on one Postgres |

## Repo layout

```
quiz-app/                       repo root (product: Quiztin)
├── README.md                   context-system front door
├── CLAUDE.md                   mandatory per-session rules (⬜)
├── context/                    the context system (this folder)
├── docs/                       specs (docs/specs/), project docs, diagrams (the original AUM corpus was retired 2026-07-24)
├── frontend/                   React + Vite SPA (✅ built — specs 0001, 0003, 0006, 0008, 0009)
├── src/                        one host + two modules (✅ modular monolith — spec 0007, folded the microservices in)
│   ├── Quiztin.Api/            the single host: serves the SPA at / and routes /api (was the Gateway + all services)
│   ├── Quiztin.Modules.Identity/    Auth + User merged: register/login/refresh/logout + UC14 profile
│   └── Quiztin.Modules.Assessment/  classrooms, quizzes, attempts, grading, AI feedback + generation (UC2/3/6/8/9)
├── tests/                      Quiztin.Api.Tests + one test project per module
├── Quiztin.sln
├── docker/  ·  docker-compose.yml   Postgres + the one app
```

**Divisibility:** the boundary is now the **module**, not a deployable service. Each module (`Identity`, `Assessment`) is a Clean/Onion unit with `Domain` / `Application` / `Infrastructure` / `Api` layers, compiled into the one `Quiztin.Api` host and isolated by a Postgres schema (`identity`, `quiz`) rather than by process. (Pre-0007 this was five independently deployable services behind a YARP gateway; see the §7 supersede notice in `foundation.md`.)

> Note: untracked pre-0007 directories (`src/Gateway/`, `src/Services/`) may still sit on disk from before the refactor. They are not in `Quiztin.sln` and not tracked by git, so they are dead weight, not part of the build.

## Boundaries / modules

Per service, dependencies point inward (§4 principle 3): **Domain** (entities, state machine, interfaces, domain events — zero deps) ← **Application** (facades, DTOs, orchestration, validation) ← **Infrastructure** (EF/Npgsql persistence, external strategies incl. the Claude client, event dispatch) ← **API** (controllers, DI, gateway-facing endpoints).

Service responsibilities:
- **AuthService** — mints HS256 JWTs; the one identity source. ✅ Built (PR #19): `AuthUser`, PBKDF2 hashing, register/login. ✅ Sessions (PR #23): `RefreshToken` (rotating, hashed, `SessionId` families, reuse detection), `refresh`/`logout`. Wires no JWT middleware or CORS by design (§ security §4). `AuthService.Tests` covers the rotation rules.
- **UserService** — user profiles (UC14), role-aware.
- **QuizService** — classrooms, enrolment, quizzes, questions, the **QuizAttempt lifecycle, and grading**. The heaviest service; the grading authority.
- **ResultService** — folded into the **Assessment** module (spec 0007); no longer a separate service. UC9/UC10 results are read/reporting **aggregated on read** from Assessment's own attempt data — no consumed event, no separate read model.
- **NotificationService** — deferred; no v1 loop step needs it.

## Data & tenancy model

- **Database-per-service** on one shared PostgreSQL instance (§7 #10). No cross-service DB joins; services talk over HTTP (via the gateway) or via events, not shared tables.
- **Tenancy is enforced in application code, not the data layer** — every classroom/quiz/attempt query is scoped by the authenticated `Guid UserId` (JWT `NameIdentifier`, §7 #14). This is a **security boundary**: an unscoped query on a tenant table is a bug (see `code-standards.md`). Making this consistent and non-bypassable is a v1 requirement.
- **EF Core conventions** (§7 #18): TPH for the Question hierarchy; `Options` as `jsonb` + `ValueComparer`; optimistic concurrency via Postgres `xmin`; `EnableRetryOnFailure` on every service.
- **Grading and reporting now share the Assessment module and its attempt tables** — results (UC9/UC10) are **aggregated on read**, not via an eventually-consistent projection (spec 0007). `QuizAttemptGradedEvent` still dispatches post-commit in-process, but the observer that would have projected it (`DashboardProjectionUpdater`) is dormant no-op code left from the pre-0007 design; results reads don't depend on it, so the at-least-once/no-outbox seam (§10) no longer applies to results.

## Keystone unlock

The **create→take→results→feedback loop** is the keystone, and it is **now unlocked** (foundation.md §9). It was blocked by (a) no valid migration for the attempt tables and (b) no results read-side; both are resolved: **(1) the Postgres migration** (regenerated fresh, all entities) landed in Layer-0 — nothing runs without it; **(2) the real scoring contract** shipped (spec 0005) — grading sees correct answers; **(3) the Assessment module's on-read results aggregation** (spec 0007 folded ResultService in) — closes the loop. Auth, the host, and the SPA wrap around this spine.

## What lives where (quick rule)
- Business logic & grading → the owning service's **Application/Domain**, never controllers.
- Shared types across the frontend & backend → duplicated deliberately (no shared package across the language boundary); within .NET, cross-service contracts are HTTP DTOs, not shared entity libraries.
- Persistence & external calls (Claude, other services) → **Infrastructure**, wrapped behind a Domain/Application interface.
- Routing, CORS, cross-cutting auth → the **gateway**.
- UI → `frontend/` only.

## Open build-time decisions
*(record each in `progress-log.md` as a `decision` when made)*
- **Resolved (spec 0007; spec 0010 §7 #35; spec 0011 §7 #36):** results are aggregated on read in the Assessment module, directly from attempt data — no projection tables, no read-through to a separate service.
- Whether the Claude client is a shared internal library or duplicated per service (QuizService generation + feedback both need it).
- **Resolved (spec 0002 §7 #30):** gateway auth depth — services stay JWT-authoritative and the gateway forwards credentials; gateway-level validation is a later "both" hardening.
- **Resolved (spec 0002 §7 #31):** databases on one Railway Postgres — one managed instance with four separate databases (authdb/userdb/quizdb/resultdb).

## Open architectural questions
- None blocking. The event-delivery reliability gap (no outbox) is a known, accepted v1 seam (§10), to revisit if results-drift shows up.
