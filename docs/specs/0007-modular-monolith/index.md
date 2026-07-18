# 0007. Modular monolith (supersedes the microservice architecture)

**Date**: 2026-07-18
**Status**: Accepted

## Summary

Quiztin was five .NET microservices (Auth, User, Quiz, Result, Notification) with a
database per service behind a YARP gateway. For a solo developer with no traffic that
was premature: five deployables, five databases, and a duplicated user identity that
produced a real bug (a first-time profile save returned a silent 500, because the same
user lived in two databases and the profile foreign key could never be satisfied). This
decision collapses the system into one ASP.NET Core app (`Quiztin.Api`) with two
compiler-enforced module boundaries, `Identity` and `Assessment`, against one Postgres
database with a schema per module. "Build like microservices, not for microservices":
clean module seams so a module could still be split out later, but one process and one
database so the distributed-systems tax is gone. The profile bug disappears by
construction, and five deployables become one.

## Decision

Run one ASP.NET Core host that serves both `/api` and the built SPA. The domain lives in
two module class libraries, each with `Domain / Application / Infrastructure / Api` as
folders (layering by convention within a module; the compiler enforces the boundary
*between* modules, since neither module references the other). One Postgres database
(`quiztin`) with a schema per module (`identity`, `quiz`), one `DbContext` and one
migration history per module. Real foreign keys within a module; cross-module references
stay plain indexed Guids (no cross-schema FK), so a module stays splittable. The Identity
module issues the JWT; the host validates it once. In-process events and the existing
GoF patterns are kept as-is; no new messaging library. One Railway service; one
docker-compose app plus Postgres.

**Implementation skills**: none.

## Proposed stack

| Layer | Choice | Reason |
|---|---|---|
| Host | one ASP.NET Core app `Quiztin.Api` (net10.0) | one origin serves `/api` + the SPA; one JWT validator |
| Modules | `Quiztin.Modules.Identity`, `Quiztin.Modules.Assessment` (class libraries) | compiler-enforced module boundary; `Domain/Application/Infrastructure/Api` folders inside |
| Database | one Postgres `quiztin`, schema per module (`identity`, `quiz`) | integrity within a module, plain Guids across; fixes the FK gap |
| Persistence | EF Core 10 + Npgsql, one `DbContext` + migration history per module | each module owns its schema and migrations |
| Auth | JWT bearer + rotating refresh cookie (unchanged); host validates once | zero SPA churn; Identity issues, host validates |
| AI | Anthropic.SDK behind the existing strategy seam (unchanged) | the feedback pipeline moved verbatim into the Assessment module |
| Frontend | React + Vite SPA (unchanged), same-origin `/api` | served by the host; Vite dev proxy targets the host |
| Deploy | one Railway service; docker-compose = app + postgres | one deployable, down from four |

## Consequences

**Positive**:
- The onboarding profile bug is fixed by construction: one users table, `Profiles.UserId`
  is a real FK to it, and registration creates that row.
- Five deployables and five databases become one; no gateway, no per-service JWT config,
  no cross-service data duplication, no eventual-consistency seam for results.
- Simpler to build, run, and reason about; the module boundaries keep the "splittable
  later" option open.

**Negative / tradeoffs**:
- One process is one failure and scaling unit. Acceptable at this stage; a genuinely hot
  module would be the signal to split it back out.
- Module boundaries are enforced only at the module-project seam, not the layer seam
  within a module (layering is by convention now, not by compiler).
- A full brand rename (QuizApp → Quiztin) happened here, reversing the earlier "defer the
  rename" decision (foundation §7 #4).

**Neutral**:
- The database is reset (no production data existed); one fresh migration per module.
- The "Quiz" module is named **Assessment** to avoid a C# namespace/type clash with the
  `Quiz` entity.

## Follow-up

- [ ] A later prose sweep of the context files may still surface stray "five services /
  gateway / database-per-service" mentions; reconcile as found (`/sync`).
- [ ] `RefreshToken.UserId` could become a real FK to `identity.AuthUsers` (same schema);
  left indexed-only for now to avoid perturbing rotation semantics.
- [ ] The canonical users table is `identity.AuthUsers` (from the rich `AuthUser`); rename
  to `users` is optional cosmetic follow-up.

## Rationale

Reasoning, the options weighed, and the premise notes live in [rationale.md](rationale.md).
Verification steps and their results are in [verify.md](verify.md).
