# Quiztin — UI Tokens

> ⏳ **PENDING.** This file will hold the design tokens, generated **from** the Claude Design export once it exists in the repo. Do not hand-author a palette here — see `claude-design-handoff.md` for how to produce the export and then generate this file.

When generated, this file will document:
- The **raw palette** (private) → **semantic aliases** (the contract components code against) → **framework binding** (e.g. Tailwind `@theme`), plus type scale, spacing, radius.
- The theming switches (e.g. `data-theme` for dark mode).
- **The invariant:** tokens only — no raw hex, no off-palette values in components (`code-standards.md` §7).

Until then: no frontend styling should hard-code colors/spacing.
