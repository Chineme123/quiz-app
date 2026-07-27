# Quiztin — `docs/`

This folder holds the project's **build specs** (`specs/`, the current design source, written via `/architect`), plus the diagrams and two archival docs left from the original design corpus.

For the *distilled, current* decisions, read `../context/foundation.md` — the source of truth — and the rest of the `../context/` system. `specs/` holds the per-slice build specs those decisions cite.

## Contents

| Path | What |
|---|---|
| `specs/` | **The build specs** (`/architect` output) and the live design record: `0001-frontend-foundation`, `0002-production-platform`, `0003-landing-page`, `0004-core-loop/` (umbrella over `0005` AI feedback, `0006` take-quiz, `0008` classroom, `0009` authoring), and `0007-modular-monolith`. Each is a folder with `index` / `rationale` / `verify`. |
| `diagrams/` | `.drawio` diagrams from the original corpus. Note: the microservice-architecture diagram predates the spec 0007 monolith. |
| `project-phases.md` | ⚠️ **Archival** — the *original* UC-catalog roadmap. Superseded; the live roadmap is `../context/build-graph.md`. |
| `project-environment-and-architecture.md` | ⚠️ **Archival** — the *original* environment (SQL Server + Codespaces + five microservices), all since replaced. For how to run today, see the root `README.md` → Getting started. |

## Status
Don't track build status here — it drifts. The live sources are `../context/build-graph.md` (what depends on what), the spec status lines and the core-loop status table in `specs/`, and `../context/progress-log.md` (what's actually built, newest first).

> The original AUM corpus — the per-UC `.docx` design docs (UC2/6/8/14) and their markdown conversions, plus the reusable design prompts — was **retired on 2026-07-24** once its intent was absorbed into `context/` and `specs/`. A fidelity audit of the build against those original UCs is preserved in `../context/progress-log.md`. The `context-system.skill` bundle that once lived here is now installed system-wide at `~/.claude/skills/context-system/`.
