# Quiztin — Code Standards

> Implementation law: how the code is written, read top-to-bottom every session. For *what* is being built see `project-overview.md`; for *why* see `foundation.md` — which wins if this file ever disagrees. Libraries live in `library-docs.md`; data-handling policy lives in `security.md` (it wins on data questions).

**Status key:** ✅ established house style · ⬜ target (adopt going forward) · ⚠️ current violation to fix.

## §1 Engineering mindset
Think before coding: read the context system first (`CLAUDE.md` lists the order). Scope is sacred — do the thing asked, one thing at a time, simplest version that works. The Layer-0 must-fixes (foundation §8) are prerequisites; don't build features on the broken migration/scoring/auth. Patterns are applied deliberately (AUM), but a pattern that adds no behavior is drift, not a deliverable (§6).

## §2 Language & style
- **C# on .NET 10** (foundation §7 #13), `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>` in every project. ✅
- **Honor nullability.** No `!` null-forgiving to silence the compiler; a nullable reference means "handle null." Avoid `dynamic`/`object` as escape hatches — types are the contract.
- **DTOs are the wire contract, entities are the domain.** One source of truth per type; never leak EF entities across a service boundary — map to a DTO (§4).
- ⚠️ **`.csproj` hygiene:** `QuizService.API.csproj` has duplicated `<Project>`/`<PropertyGroup>` headers — fix. Every `.csproj` targets `net10.0` and is pinned by the root `global.json`.

## §3 Repo & boundaries
- **Clean/Onion dependency rule** (foundation §4.3): `Domain` (zero deps) ← `Application` ← `Infrastructure` ← `API`. Dependencies point inward; nothing in Domain references EF, ASP.NET, or another service.
- **Interfaces in Domain, implementations in Infrastructure.** Repositories (`IQuizRepository`, `IProfileRepository`), strategy contracts (`IScoringStrategy`, `IQuestionGenerationStrategy`), and the event dispatcher are declared in Domain; concretes live in Infrastructure. ✅ (QuizService is the canonical layout; UserService drifted — §7 #20 says adopt QuizService's placement.)
- **No shared entity libraries across services.** Services integrate over HTTP DTOs (through the gateway) or domain events — never a shared DB table or a shared entity assembly.

## §4 ASP.NET Core / service conventions
The structuring the framework doesn't impose — impose it here.
- **Logic lives in Application services and Domain, never in controllers.** Controllers are thin: extract the principal, call one Application method, map the result to an HTTP response. `QuizAppService` / `TakeQuizFacade` are the model. ✅
- **Constructor injection only**; register services in `Program.cs`. Strategies are injected as `IEnumerable<IStrategy>` and selected by a mode/`Supports(...)` predicate (as `QuizAppService` and `UserProfileController` already do). ✅
- **Identity comes from the token, in the controller.** Read `Guid UserId` from `User.FindFirst(ClaimTypes.NameIdentifier)`; pass a **`Guid`** into services. ⚠️ Do **not** pass `string teacherId` (QuizService's current `CreateQuizAsync(..., string teacherId, ...)` and string `Classroom.TeacherId` are the identity drift to fix — foundation §7 #14); ⚠️ never fall back to a hardcoded/`Guid.Empty` default user — a missing identity is `401`.
- **Wrap every external call behind an interface in Infrastructure** — the Claude client, cross-service HTTP calls, and persistence are all Infrastructure concretes behind Domain/Application interfaces, so the Application layer stays testable and provider-agnostic.
- **DTO mapping is manual and explicit** (private `MapToDto`), and **validation is manual** via a `ValidationResult { IsSuccess, Errors }` accumulator (foundation §7 #21). ✅ No AutoMapper/FluentValidation.
- **EF conventions** (see `library-docs.md`): TPH questions via `HasDiscriminator`, `Options` as `jsonb` + `ValueComparer`, concurrency via Postgres `xmin`, `EnableRetryOnFailure` in **every** service's `AddDbContext` (⚠️ UserService lacks it). Repositories own EF; Application never touches `DbContext` directly.

## §5 Multi-tenancy (security boundary — not a style choice)
- Scope comes from the **authenticated principal**, never a client-supplied id. Every query on a tenant-owned table (Classroom, Quiz, QuizAttempt, Enrollment, Profile) is filtered by the caller's `Guid UserId` / classroom ownership.
- **An unscoped query on a tenant table is a bug**, full stop. Enrolment gating (FR7) and classroom ownership checks are enforced server-side from the token — the ownership checks in `QuizAppService` are the right idea; make them consistent and non-bypassable across every read and write. (Policy authority: `security.md` §4.)

## §6 Patterns to prefer / anti-patterns to avoid
- **Real patterns only.** State (QuizAttempt lifecycle), Strategy (scoring/feedback/generation/profile-update), Factory (Question, Strategy), Observer (graded-event fan-out), Facade (`TakeQuizFacade`) are genuine and stay. ⚠️ The **Command** (`SubmitQuizCommand`/invoker) is a hollow passthrough — either give it real behavior (queuing, undo, logging) or remove it; don't keep a pattern for its name (foundation §4.2).
- **Rich, encapsulated entities:** private setters, private EF constructor, guard clauses, behavior methods. `QuizAttempt` and `Profile` are the model; ⚠️ `Quiz` is anemic (public setters) — tighten. Never expose settable concurrency/score fields (`TotalScore`, `RowVersion` are currently public — fix).
- **Explicit over magic:** favor visible failure modes. The DI-resolved Observer and the post-commit event dispatch (no outbox) are known magic seams (foundation §9) — accepted for v1, documented, not extended silently.

## §7 Styling (frontend)
The React SPA styles from **design tokens only — no raw hex, no off-palette values** (foundation §7 #17 discipline). The token contract lives in `ui-tokens.md`. ⏳ **PENDING** until the Claude Design export exists; until then, don't hand-code a palette.

## §8 Error handling
- **No empty catches; no catch-log-nothing.** ⚠️ `UserProfileController`'s `catch (Exception) { // Log error; return 500 }` logs nothing — replace with real structured logging.
- **Prefer a global exception-handling middleware** that maps domain exceptions to HTTP + a consistent error envelope: `KeyNotFoundException → 404`, `UnauthorizedAccessException → 403`, `ArgumentException`/validation → `400`, unexpected → `500`. This replaces per-controller `try/catch` sprawl and keeps controllers thin (§4). ⬜
- **Context prefixes on thrown/logged errors**; **safe user-facing messages** (no stack traces, no internal detail, no sensitive fields — `security.md` §6). Log the exception server-side, return a generic message to the client.

## §9 Security & secrets (code-level; policy defers to `security.md`)
- **Env vars / user-secrets only** for the DB connection string, JWT signing key, and Claude API key — never hardcoded, never in the frontend, never logged. ⚠️ The hardcoded JWT key in `QuizService/Program.cs` is the reference violation to remove.
- Identity from claims (§4); tenant scoping from the principal (§5). On any data-handling question, **`security.md` wins**.

## §10 Testing posture
- **xUnit + Moq** (already in the repo). Cover the load-bearing domain first: the QuizAttempt **state-machine transitions** (legal + illegal), the **scoring** contract once redesigned, and **enrolment/ownership** gating.
- ⚠️ Delete the empty `UnitTest1.cs` placeholders (→ `quiz-trash/`). Integration tests that need a DB use a real Postgres (Testcontainers) rather than a provider that hides Npgsql-specific behavior — don't test on a different provider than you ship.

## §11 Naming, imports, comments
- `nameof(...)` for discriminators and anything that must track a rename (as `QuizDbContext` already does). ✅
- **Comments say *why*, not *what*.** No dead comments (`// Log error` that logs nothing), no `TODO` left in committed code — put follow-ups in `progress-log.md`.
- Consistent folder naming across services: `Infrastructure/Persistence` (not `Data`), interfaces in `Domain/Interfaces` — adopt QuizService's layout as canonical (foundation §7 #20).

---

These standards are comprehensive by design — read them top to bottom each session. When a rule and a shortcut conflict, the rule wins.
