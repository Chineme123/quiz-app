# CLAUDE.md — mandatory session rules

Read this first, every session. Then read the context system — `README.md` has the order, `context/foundation.md` is the source of truth. When this file and `foundation.md` conflict, `foundation.md` wins.

This is a **solo project** (foundation §0). There is no team collaboration layer (no `COLLAB.md`, no branch/PR ceremony required). The discipline below is what keeps the context system honest anyway.

## Before you write any code
1. Read `context/code-standards.md` top to bottom — it's the implementation law.
2. Read the context file(s) relevant to the work (`architecture.md` for shape, `library-docs.md` before adding a dependency, `security.md` before touching student data or the LLM).
3. Check `context/build-graph.md` for what the task depends on, and `context/progress-log.md` for what already exists.

## Before you finish (mandatory — same weight as reading first)
1. **Add a `progress-log.md` entry** for any real work. This is not optional.
2. **If you made a decision that changes `foundation.md` or another context file, update that file too** and add a `docs` progress entry noting it. Context files must never drift from what was decided (the golden rule).

## The non-negotiables (from `README.md` — never violate)
- Enrolment gates taking (FR7); tenant scope comes from the authenticated `Guid UserId` (JWT `NameIdentifier`), never the client.
- No student PII to the LLM; only task-necessary academic content (`security.md`).
- No secrets in git or logs (DB password, JWT key, Claude API key → env/user-secrets).
- Every Claude call has a deterministic fallback.

## Scope discipline
- Do the thing asked; don't widen scope mid-task. If you spot adjacent work, note it in `progress-log.md` and move on.
- The **Layer-0 must-fixes** (foundation §8: Postgres migration, real scoring contract, framework pin, identity/auth) are prerequisites — everything sits on them.

## Git workflow (solo, local-first)

Applies even though this is a solo project. The working tree stays **local** (no pushing unless the developer asks) — but the branch + commit discipline runs locally regardless. Push/PR/CI only become active once you push to a remote.

**Branches**
- Never commit directly to `main`. `main` stays green (build + tests pass).
- One branch per unit of work: `<type>/<slug>` (e.g. `feat/uc3-join-classroom`, `fix/postgres-swap`). Types match the commit types below.
- Merge to `main` with `--no-ff` once the work is done, green, and logged.

**Commits — Conventional Commits**
- Format: `<type>(<optional scope>): <imperative summary>`. Types: `feat` · `fix` · `refactor` · `chore` · `docs` · `test` · `build` · `ci` · `perf` · `style` · `revert`.
- Small, focused commits. Summary says *what*; body says *why* when not obvious.
- Enforced by the `commit-msg` hook in `.githooks/`. One-time per clone: `git config core.hooksPath .githooks`.

**Definition of done for a branch (before merging to `main`)**
1. `dotnet build QuizApp.sln` green; `dotnet test` passes.
2. A `progress-log.md` entry is added — the one ritual that never lapses.
3. Any decision that changes a context file → update that file too (the drift rule).

**When/if you push to a remote** (currently local-only by choice)
- The `pre-push` hook blocks direct pushes to `main` — push your branch and open a PR.
- PR → CI green → merge; a CI "progress-entry required" check can be added then.
