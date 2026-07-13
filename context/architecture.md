# Quiztin — Architecture

> How the pieces fit. For *why* any choice was made, see `foundation.md` (cited as §7 #N) — it wins if this file ever disagrees. Coding conventions live in `code-standards.md`; the stack details live in `library-docs.md`.

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

The SPA talks only to the gateway. Grading happens in QuizService at submission; ResultService is a **read/reporting** service that projects from QuizService's `QuizAttemptGradedEvent` and serves the results screens (§7 #8).

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
├── docs/                       the AUM design corpus, converted to markdown (✅, was "Quiz Application/")
├── frontend/                   React + Vite SPA (✅ built — spec 0001)
├── src/
│   ├── Gateway/                YARP gateway, serves the SPA + routes /api (✅ built + deployed — spec 0002)
│   └── Services/
│       ├── AuthService/        API/Application/Domain/Infrastructure + Tests (register/login/refresh/logout built)
│       ├── UserService/        (UC14 built)
│       ├── QuizService/        (UC6 + UC8 built; grading authority)
│       ├── ResultService/      (rebuild for v1: UC9/UC10 read side)
│       └── NotificationService/(deferred)
├── quiz-trash/                 cut scaffolds, final cleanup step (⬜)
├── QuizApp.sln
└── docker-compose.yml          Postgres + services
```

**Divisibility:** each service is an independently buildable/deployable 4-project Clean Architecture unit (`*.API` / `*.Application` / `*.Domain` / `*.Infrastructure`).

## Boundaries / modules

Per service, dependencies point inward (§4 principle 3): **Domain** (entities, state machine, interfaces, domain events — zero deps) ← **Application** (facades, DTOs, orchestration, validation) ← **Infrastructure** (EF/Npgsql persistence, external strategies incl. the Claude client, event dispatch) ← **API** (controllers, DI, gateway-facing endpoints).

Service responsibilities:
- **AuthService** — mints HS256 JWTs; the one identity source. ✅ Built (PR #19): `AuthUser`, PBKDF2 hashing, register/login. ✅ Sessions (PR #23): `RefreshToken` (rotating, hashed, `SessionId` families, reuse detection), `refresh`/`logout`. Wires no JWT middleware or CORS by design (§ security §4). `AuthService.Tests` covers the rotation rules.
- **UserService** — user profiles (UC14), role-aware.
- **QuizService** — classrooms, enrolment, quizzes, questions, the **QuizAttempt lifecycle, and grading**. The heaviest service; the grading authority.
- **ResultService** — **read/reporting only** (UC9/UC10). Consumes `QuizAttemptGradedEvent` into a read model; never grades. (Rebuild from scaffold.)
- **NotificationService** — deferred; no v1 loop step needs it.

## Data & tenancy model

- **Database-per-service** on one shared PostgreSQL instance (§7 #10). No cross-service DB joins; services talk over HTTP (via the gateway) or via events, not shared tables.
- **Tenancy is enforced in application code, not the data layer** — every classroom/quiz/attempt query is scoped by the authenticated `Guid UserId` (JWT `NameIdentifier`, §7 #14). This is a **security boundary**: an unscoped query on a tenant table is a bug (see `code-standards.md`). Making this consistent and non-bypassable is a v1 requirement.
- **EF Core conventions** (§7 #18): TPH for the Question hierarchy; `Options` as `jsonb` + `ValueComparer`; optimistic concurrency via Postgres `xmin`; `EnableRetryOnFailure` on every service.
- **Grading→reporting** is **eventually consistent via a domain event** — QuizService commits the graded attempt, then dispatches `QuizAttemptGradedEvent` post-commit; ResultService projects it. This is an accepted at-least-once seam (no outbox in v1, §10).

## Keystone unlock

The **create→take→results→feedback loop** is the keystone. It's blocked today by (a) no valid migration for the attempt tables and (b) no results read-side. So the unlocks are, in order: **(1) the Postgres migration** (regenerate fresh, all entities) — nothing runs without it; **(2) the real scoring contract** — grading must see correct answers; **(3) ResultService's read projection** — closes the loop. Auth, the gateway, and the SPA wrap around this spine.

## What lives where (quick rule)
- Business logic & grading → the owning service's **Application/Domain**, never controllers.
- Shared types across the frontend & backend → duplicated deliberately (no shared package across the language boundary); within .NET, cross-service contracts are HTTP DTOs, not shared entity libraries.
- Persistence & external calls (Claude, other services) → **Infrastructure**, wrapped behind a Domain/Application interface.
- Routing, CORS, cross-cutting auth → the **gateway**.
- UI → `frontend/` only.

## Open build-time decisions
*(record each in `progress-log.md` as a `decision` when made)*
- Exact ResultService read-model shape (denormalized projection tables vs. read-through to QuizService for v1).
- Whether the Claude client is a shared internal library or duplicated per service (QuizService generation + feedback both need it).
- **Resolved (spec 0002 §7 #30):** gateway auth depth — services stay JWT-authoritative and the gateway forwards credentials; gateway-level validation is a later "both" hardening.
- **Resolved (spec 0002 §7 #31):** databases on one Railway Postgres — one managed instance with four separate databases (authdb/userdb/quizdb/resultdb).

## Open architectural questions
- None blocking. The event-delivery reliability gap (no outbox) is a known, accepted v1 seam (§10), to revisit if results-drift shows up.
