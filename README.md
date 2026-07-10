# Quiztin

A classroom quiz web app — teachers author and publish quizzes inside classrooms, enrolled students take them and are scored immediately, and both sides review question-by-question results with **AI-generated feedback**. Built as coursework under the **Agile Unified Methodology (AUM)** (design-first, use-case-by-use-case, GRASP/GoF patterns), with the intent of a real, shippable product.

> `Quiztin` is the product/brand name. The code (solution, services) still uses the older `QuizApp` / `QuizService` identifiers — renaming them is an optional later refactor, not required work.

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
Foundation converged (v3); the full context system is written, and the **UI trio is generated** from the Claude Design export (`design-system/`) — nothing PENDING. Repo is **public** with CI + PR/auto-merge. Layer-0 backend is underway (framework pin, live Postgres migration, and secrets rotation done; identity/auth + scoring redesign remain). The frontend (`frontend/`) isn't scaffolded yet. See `context/progress-log.md` for the running state.
