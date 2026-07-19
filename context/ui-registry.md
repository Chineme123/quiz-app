# Quiztin — UI Registry

> The component registry. **Check this before building any component.** Generated 2026-07-09 from the Claude Design export (`design-system/`). Tokens: `ui-tokens.md`; composition rules: `ui-rules.md`.

**Status legend:** ⬜ planned (in the export, not yet in `frontend/`) · 🟡 in progress · ✅ built (lives in `frontend/`).

**About the export:** `design-system/` ships the **token layer + component specs/docs + a bundled `_ds_bundle.js`**. Individual component **source** (`.jsx`) is *not* in the export zip — so "port" means **build it in `frontend/` per the export's `readme.md` + component specs**, matching the tokens and rules. **The foundation primitives now exist:** spec 0001 (tasks 3-5) built the first six registry components — `Icon`, `Button`, `TextField`, `Select`, `Card`, `Toast` (with `ToastProvider` + `useToast`) — under `frontend/src/components/ui/`, on a shared `Field` chrome that backs `TextField`/`Select` (and is *not* itself a registry row). `Dialog` was ported later, for the classroom archive confirm (spec 0008). The other 10 components below remain `⬜ planned`.

| Component | Group | Status | Built path | Purpose |
|---|---|---|---|---|
| `Icon` | foundation | ✅ | `frontend/src/components/ui/Icon.tsx` | Phosphor wrapper — consistent sizing + a11y (decorative by default, `label` when meaningful) |
| `Button` | actions | ✅ | `frontend/src/components/ui/Button.tsx` | primary / secondary / ghost actions |
| `IconButton` | actions | ⬜ | — | icon-only action |
| `TextField` | forms | ✅ | `frontend/src/components/ui/TextField.tsx` | text input |
| `Select` | forms | ✅ | `frontend/src/components/ui/Select.tsx` | dropdown |
| `Checkbox` | forms | ⬜ | — | multi-select |
| `Radio` | forms | ⬜ | — | single choice |
| `Switch` | forms | ⬜ | — | toggle |
| `Badge` | feedback | ⬜ | — | status badge (quiz states: available/scheduled/completed/closed) |
| `ProgressBar` | feedback | ⬜ | — | **core** — calm, ever-present take-quiz progress |
| `Toast` | feedback | ✅ | `frontend/src/components/ui/Toast.tsx` | transient confirmation (e.g. "Quiz published") |
| `Tooltip` | feedback | ⬜ | — | hint on hover/focus |
| `Card` | surfaces | ✅ | `frontend/src/components/ui/Card.tsx` | **the primary surface** (§6 of `ui-rules.md`) |
| `Dialog` | surfaces | ✅ | `frontend/src/components/ui/Dialog.tsx` | modal — publish-confirm, submit-confirm (warm blurred scrim). Ported for the archive confirm (spec 0008); adds a focus trap + focus restore the export's prototype lacks |
| `Tabs` | navigation | ⬜ | — | section switching |
| `AnswerChoice` | quiz | ⬜ | — | **domain core** — selectable answer tile carrying both *answering* (idle/selected) and *reviewing* (correct/incorrect/missed) |
| `AIFeedbackCard` | quiz | ⬜ | — | **domain core** — the distinct, supportive AI companion voice (a helpful voice, not a grade stamp) |
| `ResultSummary` | quiz | ⬜ | — | **domain core** — encouraging post-submit score card (headline + calm ring + counts as "to review") |

**Export-marked starting points:** `Button`, `Card`, `ProgressBar`, `AnswerChoice`, `AIFeedbackCard`, `ResultSummary`.

**Reference UI kits (flows in the export docs):** teacher (Dashboard → Quiz editor → Class results, with publish-confirm Dialog + success Toast); student (Home → calm Take-quiz flow → Results review with AI feedback). These map onto UC6/UC8/UC9/UC10.

**The rule:** before building any component, check this registry — **reuse** if built (✅), **port from `design-system/`** if planned (⬜). If it's **not in the export, it hasn't been designed** — design it (or request it from Claude Design) first; don't improvise off-system.
