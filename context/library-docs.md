# Quiztin — Library Docs

> Each dependency and how *this project* uses it. Conventions live in `code-standards.md`; the reasoning behind each stack choice lives in `foundation.md` §7. This file describes the **target** stack (the locked decisions); where the current code still differs, it's marked ⚠️.

**Status key:** ✅ locked / in use · 🕗 TBD (decide when we get there) · ⚠️ current-state drift to reconcile

## Backend — .NET

### .NET 10 SDK — ✅
- **Why:** foundation §7 #13 (finish the started upgrade; tooling already on 10). Pinned by a root `global.json`.
- **How used:** every service + the gateway target `net10.0`, `Nullable` + `ImplicitUsings` enabled.
- **Gotchas:** ✅ done — all `.csproj` target `net10.0` with EF/JwtBearer `10.x`, the duplicated `QuizService.API.csproj` headers are fixed, and a root `global.json` pins the SDK (`10.0.301`) so every machine resolves the same toolchain.

### EF Core 10 + Npgsql (`Npgsql.EntityFrameworkCore.PostgreSQL`) — ✅
- **Why:** foundation §7 #10/#18 — PostgreSQL is Railway-native; Npgsql is the EF provider.
- **How used:** database-per-service; `DbContext` per service (`QuizDbContext` is the model). TPH question inheritance via `HasDiscriminator` (`nameof` values); `MultipleChoiceQuestion.Options` stored as **`jsonb`** with an explicit `ValueComparer` for list change-tracking; optimistic concurrency via the Postgres **`xmin`** system column (`entity.UseXminAsConcurrencyToken()`); `EnableRetryOnFailure` in **every** service's `AddDbContext`.
- **Gotchas:** ✅ done — every service is on `UseNpgsql` with `xmin` concurrency and `jsonb` `Options`; the Postgres `InitialCreate` migrations were regenerated (attempt-side tables included) and applied; UserService gained `EnableRetryOnFailure`. Keep the idempotency transaction inside the execution strategy (foundation §7 #19).

### ASP.NET Core Web API — ✅
- **How used:** five services + the gateway; thin controllers, Application services own logic (`code-standards.md` §4). Swagger per service in Development only.
- **Gotchas:** the dev-time seeder (`DataSeeder`) runs from `Program.cs` in Development and calls `MigrateAsync()`; the Postgres migrations exist now, so it runs cleanly. ⚠️ spec 0002 pulls `MigrateAsync` out of the Development-only seeder into an env-gated startup hook (`RUN_MIGRATIONS_ON_STARTUP`), so production applies migrations without seeding demo data.

### Microsoft.AspNetCore.Authentication.JwtBearer — ✅
- **Why:** foundation §7 #15 — HS256 JWTs, AuthService issues, all services validate.
- **How used:** shared `ValidIssuer` + `ValidAudience` (both `quiztin`) + `IssuerSigningKey` across services, from configuration (`JwtSettings__Secret`).
- **Gotchas:** the secret must be identical across Auth, User and Quiz or validation fails with a misleading 401. Per spec 0001 the access token drops to ~15 min (`AuthTokens__AccessTokenMinutes`); `ValidateLifetime` stays on and the default 5-minute clock skew still applies.

### System.IdentityModel.Tokens.Jwt — ✅
- **Why:** AuthService mints the tokens (`JwtTokenService`); this is the writing side of the JwtBearer pair.
- **How used:** `JwtSecurityTokenHandler` builds the HS256 token; claims are `NameIdentifier` (the canonical `Guid`, foundation §7 #14), `Email`, `Role`.
- **Gotchas:** the expiry now comes from `AuthTokens:AccessTokenMinutes` (default 15), not the old hardcoded `AddHours(8)` (built PR #23). The frontend never decodes this token — `/api/auth/refresh` returns `userId` and `role` in the body precisely so the SPA takes no JWT-parsing dependency.

### YARP (`Yarp.ReverseProxy` 2.3.0) — ✅ built + deployed (spec 0002)
- **Why:** foundation §7 #16 — the single origin. Routes `/api/*` to the services and serves the SPA same-origin, which the in-memory-token + `HttpOnly` refresh-cookie design (§7 #26) requires.
- **How used:** `src/Gateway/` (net10.0) loads routes/clusters from `appsettings.json` (`AddReverseProxy().LoadFromConfig`), serves `wwwroot` with an `index.html` fallback, and answers `/health`. Production overrides the cluster addresses via env (`ReverseProxy__Clusters__<id>__Destinations__primary__Address` → Railway private domains). **No CORS anywhere** (§7 #27).
- **Gotchas:** the gateway **forwards** credentials; each service stays JWT-authoritative (§7 #30), so no auth logic lives in the gateway. Deploy the gateway **last** — deploying it before the services are up leaves its process caching failed private-DNS lookups (502) until a redeploy.

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

## Frontend — React + Vite (stack settled by spec 0001; exact minors pinned at scaffold via `npm view`)

### React + Vite + TypeScript (SPA) — ✅ (choice) / 🕗 (not yet scaffolded)
- **Why:** foundation §7 #5, #23. Lives in `frontend/`, its own toolchain, not in `QuizApp.sln` and not in CI until a Node job is added.
- **Gotchas:** the Vite dev proxy is load-bearing, not a convenience — there is no CORS in any service (§7 #27). Route the proxy on specific prefixes (`/api/auth`, `/api/profile`, `/api/quizzes`…), never an `/api` catch-all, or auth calls reach the wrong service. Needs `changeOrigin: true` and `cookieDomainRewrite: ''` so the refresh cookie survives. Target the **http** launch profiles (5005 / 5079 / 5224); `UseHttpsRedirection()` is on everywhere.

### Tailwind CSS v4 (`tailwindcss`, `@tailwindcss/vite`) — ✅ (new)
- **Why:** foundation §7 #24 — binds utilities to the design tokens by reference, enforcing "no raw hex" at compile time.
- **How used:** `@theme inline { --color-primary: var(--primary); … }` over the semantic aliases only. Import `tailwindcss/theme.css` + `tailwindcss/utilities.css`; **never bare `tailwindcss`**, which would pull in Preflight and fight `design-system/tokens/base.css`.
- **Gotchas:** ⚠️ Preflight is also what sets `border-style: solid` globally. Without it every Tailwind `border` utility renders **invisible**. Restore it with a three-line `@layer base` rule. Declare `@layer theme, base, components, utilities;` first, or the design system reset outranks the utilities.

### React Router v7 — ✅ (new)
- **How used:** `createBrowserRouter` for routing only (nested routes, `errorElement`, lazy routes). **Loaders and actions stay unused** — TanStack Query owns server state (§7 #25).
- **Gotchas:** do not adopt *framework mode*; its Vite plugin and server rendering are what #5 rejected with Next.js.

### TanStack Query v5 — ✅ (new)
- **How used:** all server state. Query keys centralized; never retry a 4xx.
- **Gotchas:** the 401 → silent refresh → retry-once flow belongs in the fetch wrapper, **not** in Query's `retry`, or concurrent queries each fire their own refresh and trip the reuse detector.

### React Hook Form + Zod v4 (`@hookform/resolvers`) — ✅ (new)
- **How used:** forms, plus Zod schemas validating every response at the API boundary. Responses are **camelCase**.
- **Gotchas:** `PUT /api/profile` returns a bare array of strings on validation failure, so errors are mapped back to fields by message prefix. Brittle — see spec 0001 Follow-up.

### @phosphor-icons/react — ✅ (new)
- **Why:** the icon set the design system was drawn against. `aria-hidden` unless given a label.

### Vitest + Testing Library + vitest-axe — ✅ (new, test)
- **How used:** component tests and an automated accessibility floor on the primitives.

## Approved dependencies

Do not install anything outside this list without adding it here first (with a why + how-used).

| Package | Purpose | Status |
|---|---|---|
| .NET 10 SDK | Runtime/build for all backend services | ✅ (pin via `global.json`) |
| Microsoft.EntityFrameworkCore (10.x) | ORM | ✅ |
| Npgsql.EntityFrameworkCore.PostgreSQL (10.x) | Postgres provider | ✅ |
| Microsoft.EntityFrameworkCore.Design (10.x) | Migrations tooling | ✅ |
| Microsoft.AspNetCore.Authentication.JwtBearer (10.x) | JWT validation | ✅ |
| System.IdentityModel.Tokens.Jwt (8.x) | JWT issuing (AuthService) | ✅ |
| Yarp.ReverseProxy (2.x) | API gateway | ✅ (new) |
| Swashbuckle.AspNetCore (6–7.x) | Swagger UI (dev) | ✅ |
| Anthropic Claude client (typed HttpClient wrapper, or `Anthropic.SDK`) | AI generation + feedback | 🕗 mechanism |
| xUnit, Moq, coverlet | Testing | ✅ |
| React 19, Vite 8, TypeScript | Frontend SPA | ✅ (spec 0001) |
| tailwindcss 4 + @tailwindcss/vite | Styling, bound to design tokens | ✅ (spec 0001) |
| react-router 7 | Routing (data router, no loaders) | ✅ (spec 0001) |
| @tanstack/react-query 5 | Server state | ✅ (spec 0001) |
| react-hook-form + @hookform/resolvers + zod 4 | Forms and boundary validation | ✅ (spec 0001) |
| @phosphor-icons/react 2 | Icons | ✅ (spec 0001) |
| vitest, @testing-library/react, vitest-axe | Frontend tests + a11y floor | ✅ (spec 0001) |
| eslint + typescript-eslint + eslint-plugin-jsx-a11y + prettier | Lint (a11y rules enforced) | ✅ (spec 0001) |
| UI components | Authored in-repo from `design-system/` | ✅ no library (export ships no React source) |

**Explicitly rejected**: FluentValidation, AutoMapper (foundation §7 #21 — validation and mapping are manual). React Router *framework mode* and any SSR meta-framework (§7 #5, #25). Any mock/MSW layer (§7 #28).
