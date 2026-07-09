# Quiztin — Design Docs (AUM corpus)

The Agile Unified Methodology design ledger — the per-use-case design work that precedes code. Converted from the original Word docs to markdown (2026-07-09) for easy reading; the original `.docx` files are archived in `quiz-trash/Quiz Application (original docx corpus)/`.

For the *distilled, current* decisions, read `../context/foundation.md` — it's the source of truth. These docs are the **design detail** behind it (expanded flows, scenario tables, patterns, UI briefs).

## Contents

| File | What |
|---|---|
| `project-phases.md` | The use-case roadmap (phases, UC ordering, FR mapping) |
| `reusable-uc-design-prompts.md` | The 13-step AUM design playbook (the proto-context-system) |
| `project-environment-and-architecture.md` | Environment, encountered problems & solutions, terminal cheat-sheet |
| `uc02-create-classroom.md` | ⚠️ Placeholder — original was broken; needs a real design pass |
| `uc06-create-classroom-quiz.md` | UC6 full design (business → expanded UC → scenario tables → patterns) |
| `uc06-ui-ux-brief.md` | UC6 UI/UX design brief |
| `uc06-prompt-compilation.md` | The prompts used to produce UC6 |
| `uc08-take-classroom-quiz.md` | UC8 full design |
| `uc08-ui-ux-brief.md` | UC8 UI/UX design brief |
| `uc14-create-user-profile.md` | UC14 full design |
| `uc14-ui-ux-brief.md` | UC14 UI/UX design brief |
| `diagrams/` | `.drawio` diagrams (microservice architecture + general) |
| `context-system.skill` | The context-system skill bundle (the tool that built `context/`) |

## Status vs. code
Built: UC6, UC8, UC14. In v1 scope but undesigned/broken: UC2 (create classroom), UC3 (join). Deferred: UC4/5/7/9–13/15–19. See `../context/build-graph.md` and `../context/foundation.md` §8.

> Note (`foundation.md` §11): these docs live only on this machine by the developer's deliberate local-only choice — an accepted tradeoff, not an oversight.
