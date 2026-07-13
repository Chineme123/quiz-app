# Quiztin

A classroom quiz web app — teachers author and publish quizzes inside classrooms, enrolled students take them and are scored immediately, and both sides review question-by-question results with **AI-generated feedback**. Built as coursework under the **Agile Unified Methodology (AUM)** (design-first, use-case-by-use-case, GRASP/GoF patterns), with the intent of a real, shippable product.

> `Quiztin` is the product/brand name. The code (solution, services) still uses the older `QuizApp` / `QuizService` identifiers — renaming them is an optional later refactor, not required work.

---

## Getting started

**Prerequisites:** Docker Desktop, the **.NET 10 SDK** (for host-run migrations and `dotnet test`), and **Node 20** (for the web app).

```bash
# 1. Secrets: copy the template, then set a JWT signing key + a Postgres password
cp .env.example .env

# 2. Bring up Postgres + the backend services (one command)
docker compose up

# 3. Run the web app with hot reload (proxies /api to the gateway)
cd frontend && npm ci && npm run dev
```

The **YARP gateway** is the single origin: it serves the web app and forwards `/api/*` to the right service, so the in-memory access token + `HttpOnly` refresh cookie work same-origin. The containerised local flow and the gateway are settled by [`docs/specs/0002-production-platform/`](docs/specs/0002-production-platform/index.md); its [`verify.md`](docs/specs/0002-production-platform/verify.md) carries the migration and test commands. Backend build/test from the repo root: `dotnet build QuizApp.sln` and `dotnet test`.

---

## The context system (read this before writing any code)

This repo is driven by a **context system**: a set of source-of-truth markdown files an AI coding agent (and you) read before touching code, so decisions stay consistent across sessions and don't drift. **`context/foundation.md` is the source of truth**; every other file references it and none restate it.

### Files

| File | Job | Status |
|---|---|---|
| `context/foundation.md` | Every locked decision, with reasoning — **start here** | ✅ converged (v3) |
| `context/project-overview.md` | Plain-English digest; summarizes, never decides | ✅ |
| `context/architecture.md` | How the pieces fit; the keystone unlock | ✅ |
| `context/security.md` | Data-handling authority (student data + the LLM) — **wins on data questions** | ✅ |
| `context/code-standards.md` | Implementation law; read top-to-bottom every session | ✅ |
| `context/library-docs.md` | The stack as used here + approved dependencies | ✅ |
| `context/build-graph.md` | Dependency map (what needs what — not a timeline) | ✅ |
| `context/progress-log.md` | Living build record; add an entry after any work | ✅ |
| `CLAUDE.md` (root) | Mandatory per-session rules (read-first, log-after) | ✅ |
| `context/claude-design-handoff.md` | Claude Design intake + the prompt to generate the UI trio | ✅ |
| `context/ui-tokens.md` | Design tokens (from the export) | ✅ |
| `context/ui-rules.md` | How tokens compose into UI | ✅ |
| `context/ui-registry.md` | Component registry | ✅ |
| `design-system/` | The Claude Design export — tokens, `styles.css`, brand docs (token source of truth) | ✅ |

### Reading order
`foundation` → `project-overview` → `architecture` → `security` → `code-standards` → `library-docs` → `build-graph` → `progress-log`, then the UI trio once it exists.

### If you're here to…
| Need | Read |
|---|---|
| Understand what this is | `project-overview.md`, then `foundation.md` for the why |
| Write any code | `CLAUDE.md`, then `code-standards.md` (every session) |
| Handle student data or call the LLM | `security.md` (it wins on data questions) |
| Build UI | the UI trio — check `ui-registry.md` before building any component |
| Add a dependency | `library-docs.md` (and add it to the approved list first) |
| Decide what to build next | `build-graph.md` |
| See what exists already | `progress-log.md` |

## The golden rule
When a decision changes, update `foundation.md` **first**, then ripple the change into every file that references it, and add a `progress-log.md` entry. Never let two files disagree.

## Non-negotiables
- **Enrolment gates taking (FR7).** A student may only take a quiz in a classroom they're enrolled in — enforced from the authenticated identity, never a client-supplied ID.
- **Tenant scope comes from the token, not the client.** Every query on a classroom/quiz/attempt is scoped by the authenticated `Guid UserId` (JWT `NameIdentifier`). An unscoped query on a tenant table is a bug, not a style issue.
- **No student PII leaves to the LLM.** Only task-necessary academic content is sent to Claude; never student name/email/UserId (see `security.md`).
- **No secrets in git.** DB passwords, JWT signing keys, and API keys live in env/user-secrets — never committed, never logged.
- **The AI loop must degrade, not break.** Every Claude call has a deterministic fallback.

## Status
Foundation converged (v3); the context system is written, and the **UI trio is generated** from the Claude Design export (`design-system/`). Repo is **public** with CI + branch protection + auto-merge. **Layer-0 is complete** (framework pin, PostgreSQL migration, real scoring, identity/auth). The **frontend is built** — spec 0001 (auth + Manage Profile screens, 30 tests). Now in progress: the **production platform** — spec 0002 (YARP gateway, containerised local dev, expanded CI, deploy to Railway). See `context/progress-log.md` for the running state.
