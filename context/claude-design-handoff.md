# Quiztin — Claude Design handoff

Two outputs. The first feeds Claude Design's "Set up your design system" screen. The second runs in the repo *after* the export exists, to generate the UI trio (`ui-tokens.md` / `ui-rules.md` / `ui-registry.md`) from it.

---

## 1) Claude Design intake

Fill this into Claude Design's "Set up your design system" screen. Only the first field and the notes are things you write; the rest are files you attach.

**Company name and blurb (or name of design system)**
> **Quiztin** — a classroom quiz web app where teachers author and publish quizzes inside classrooms and enrolled students take them, get scored immediately, and review question-by-question results with AI-generated feedback. Runs as a React web app (teacher + student surfaces).

**Examples of your design system and products (all optional — attach if you have them):**
- Link the repo (or attach the `frontend/` folder) once the SPA is scaffolded.
- The drawio diagrams (`docs/diagrams/`) and the per-UC UI/UX briefs (`docs/uc0*-ui-ux-brief.md`), if useful as flow references.
- Any fonts / logo / assets you want to use.

**Any other notes? (aesthetic direction + brand voice)** — ✅ decided 2026-07-09: **friendly & approachable**
> Quiztin should feel like a **helpful, encouraging companion — not a bureaucratic testing tool.** Warm, rounded forms; generous spacing; soft, welcoming color; encouraging, plain-language copy. Personable (the name "Quiztin" reads as a friendly character), but never so playful it undermines credibility for teachers.
>
> Non-negotiable regardless of styling: the **take-quiz experience stays calm and low-anxiety** (clear progress, gentle state feedback, confirmation before irreversible actions like submit), and the whole app is **accessible** (high contrast, keyboard-navigable, screen-reader friendly). **AI feedback reads as supportive and distinct** — a helpful voice, not a grade stamp.
>
> (Palette, corner radius, and type are for Claude Design to propose within this direction — not pre-specified here.)

---

## 2) In-repo prompt — generate the UI trio from the export

Give this to Claude Code once the Claude Design export is committed:

> Read the Claude Design export in this repo (at `{path/to/export}`) and the context system in `context/`. Generate/replace three files, each referencing `foundation.md` for the *why* and never restating it:
>
> - `context/ui-tokens.md` — the raw tokens from the export (color, type scale, spacing, radius, and any others present); values come from the export, not assumption. Document the layered architecture: raw palette (private) → semantic aliases (the contract components code against) → framework binding (e.g. Tailwind `@theme`). State the theming switches (e.g. `data-theme`) and the invariant: **tokens only — no raw hex or off-palette values in components.**
> - `context/ui-rules.md` — how those tokens compose into UI. Open with a §0 prime directive derived from the brand voice in `foundation.md`, then layout/density, hierarchy, color discipline, per-surface rules (teacher authoring / student take-quiz / results + AI feedback), and required interaction states (default/loading/disabled/error/empty). Fold in the take-quiz calm/low-anxiety and accessibility intent already noted in the stub.
> - `context/ui-registry.md` — the component registry with a status legend (⬜/🟡/✅) and per-component rows: name, status, built path (`—` until ported into `frontend/`), variants, purpose. Include the rule: check this registry before building any component.
>
> Anything the export doesn't cover, mark TBD — do not invent it. When done, drop the ⏳ PENDING markers on these three files in `README.md`, and check that every cross-reference across the system still resolves.
