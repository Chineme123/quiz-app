# Quiztin — Library Docs

> Each dependency and how *this project* uses it. Conventions live in `code-standards.md`; the reasoning behind each stack choice lives in `foundation.md` §7. This file describes the **target** stack (the locked decisions); where the current code still differs, it's marked ⚠️.

**Status key:** ✅ locked / in use · 🕗 TBD (decide when we get there) · ⚠️ current-state drift to reconcile

## Backend — .NET

### .NET 10 SDK — ✅
- **Why:** foundation §7 #13 (finish the started upgrade; tooling already on 10). Pinned by a root `global.json`.
- **How used:** every service + the gateway target `net10.0`, `Nullable` + `ImplicitUsings` enabled.
- **Gotchas:** ⚠️ service `.csproj` files currently say `net8.0` and reference EF/JwtBearer `8.0.0`; bump all to 10.x. ⚠️ `QuizService.API.csproj` has duplicated `<Project>`/`<PropertyGroup>` headers — fix while bumping. Add `global.json` so the Codespace and any machine resolve the same SDK.

### EF Core 10 + Npgsql (`Npgsql.EntityFrameworkCore.PostgreSQL`) — ✅
- **Why:** foundation §7 #10/#18 — PostgreSQL is Railway-native; Npgsql is the EF provider.
- **How used:** database-per-service; `DbContext` per service (`QuizDbContext` is the model). TPH question inheritance via `HasDiscriminator` (`nameof` values); `MultipleChoiceQuestion.Options` stored as **`jsonb`** with an explicit `ValueComparer` for list change-tracking; optimistic concurrency via the Postgres **`xmin`** system column (`entity.UseXminAsConcurrencyToken()`); `EnableRetryOnFailure` in **every** service's `AddDbContext`.
- **Gotchas:** ⚠️ current code uses `UseSqlServer`, `IsRowVersion()` (SQL Server `rowversion`), and `JsonSerializer`-to-`nvarchar` for Options — all change under Npgsql: `UseNpgsql`, drop `IsRowVersion` for `xmin`, and let Npgsql map the list to `jsonb`. ⚠️ **Regenerate migrations from scratch against Postgres** — the existing SQL Server migration is stale (missing the attempt-side tables) *and* provider-specific. This is the Layer-0 step-one (foundation §8). ⚠️ UserService currently has no `EnableRetryOnFailure` — add it. Keep the idempotency transaction inside the execution strategy (foundation §7 #19).

### ASP.NET Core Web API — ✅
- **How used:** five services + the gateway; thin controllers, Application services own logic (`code-standards.md` §4). Swagger per service in Development only.
- **Gotchas:** the dev-time seeder runs from `Program.cs` in Development (`DataSeeder`) and calls `MigrateAsync()` — it will throw until the Postgres migration is regenerated.

### Microsoft.AspNetCore.Authentication.JwtBearer — ✅
- **Why:** foundation §7 #15 — HS256 JWTs, AuthService issues, all services validate.
- **How used:** shared `ValidIssuer` + `ValidAudience` + `IssuerSigningKey` across services, from configuration.
- **Gotchas:** ⚠️ key is currently hardcoded in `QuizService/Program.cs` and the audience disagrees between services (`quiz-app` vs `http://localhost:5000`) — unify and move the key to secrets (`security.md` §3). No service issues tokens yet; AuthService must be built.

### YARP (`Yarp.ReverseProxy`) — ✅ (new)
- **Why:** foundation §7 #16 — single frontend origin, centralizes routing + CORS + (optionally) JWT validation.
- **How used:** a new `src/Gateway/` .NET app routes `/api/{service}/…` to each service; CORS is configured here for the SPA origin.
- **Gotchas:** decide whether the gateway validates the JWT and forwards claims, or services re-validate (or both — recommended: both). Keep route config in `appsettings`, not hardcoded.

### Anthropic Claude client — 🕗 (mechanism to confirm)
- **Why:** foundation §7 #6 — question generation + per-question feedback, each with a deterministic fallback.
- **Recommended:** a thin **typed `HttpClient` wrapper** around the Messages API, living in QuizService Infrastructure behind `IQuestionGenerationStrategy` / the feedback strategy interface — matches the "few deps, explicit" house style (§7 #21) and keeps full control over the request shape and the data-minimization rules (`security.md` §2). Alternative: a community `Anthropic.SDK` NuGet (less code, another dependency).
- **Gotchas:** the model id is a **config value**, not hardcoded (default to a current cost-effective Claude model for feedback; a stronger one for generation if quality demands). API key from secrets, only in the calling service. Validate model output before persisting (`security.md` §1). Always wire the fallback path first so the feature works before the model is connected.

### Swashbuckle.AspNetCore — ✅ (dev)
- **How used:** Swagger UI per service for manual testing in Development.
- **Gotchas:** standardize the Bearer security scheme across services (QuizService registers it as `ApiKey`-type Bearer; keep one consistent definition).

### xUnit + Moq — ✅ (test)
- **How used:** unit tests for the domain (state machine, scoring, gating). `QuizAttemptTests` exists.
- **Gotchas:** ⚠️ remove empty `UnitTest1.cs` placeholders. DB integration tests use real Postgres (Testcontainers), not a substitute provider (`code-standards.md` §10).

## Frontend — React + Vite (🕗 versions to confirm at scaffold time)

### React + Vite (SPA) — ✅ (choice) / 🕗 (not yet scaffolded)
- **Why:** foundation §7 #5. Lives in `frontend/`, its own toolchain, not in `QuizApp.sln`.
- **Likely companions (confirm when scaffolding):** React Router (routing), TanStack Query (server state / data fetching against the gateway), Zod (form/response validation), and a styling layer bound to the design tokens. **Styling + component library are PENDING the Claude Design export** (`ui-tokens.md` / `ui-rules.md` / `ui-registry.md`) — don't hand-pick a palette or component kit before it exists.

## Approved dependencies

Do not install anything outside this list without adding it here first (with a why + how-used).

| Package | Purpose | Status |
|---|---|---|
| .NET 10 SDK | Runtime/build for all backend services | ✅ (pin via `global.json`) |
| Microsoft.EntityFrameworkCore (10.x) | ORM | ✅ |
| Npgsql.EntityFrameworkCore.PostgreSQL (10.x) | Postgres provider | ✅ |
| Microsoft.EntityFrameworkCore.Design (10.x) | Migrations tooling | ✅ |
| Microsoft.AspNetCore.Authentication.JwtBearer (10.x) | JWT validation | ✅ |
| Yarp.ReverseProxy (2.x) | API gateway | ✅ (new) |
| Swashbuckle.AspNetCore (6–7.x) | Swagger UI (dev) | ✅ |
| Anthropic Claude client (typed HttpClient wrapper, or `Anthropic.SDK`) | AI generation + feedback | 🕗 mechanism |
| xUnit, Moq, coverlet | Testing | ✅ |
| React, Vite | Frontend SPA | ✅ (choice) |
| React Router, TanStack Query, Zod | Routing, data, validation | 🕗 confirm at scaffold |
| Styling + components | — | ⏳ PENDING Claude Design export |

**Explicitly rejected** (foundation §7 #21): FluentValidation, AutoMapper — validation and mapping are manual.
