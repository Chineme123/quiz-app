# Quiztin Design System

Quiztin is a **classroom quiz web app**. Teachers author and publish quizzes inside their classrooms; enrolled students take them, get scored immediately, and review their results question-by-question with **AI-generated feedback** from "Quiztin", a friendly companion voice. It's a React web app with two surfaces — a **teacher** side and a **student** side.

This design system exists so any designer or agent can produce on-brand Quiztin screens, components, and marketing without re-deriving the look and voice each time.

> **Brand promise:** a helpful, encouraging *companion* — not a bureaucratic testing tool. Warm, rounded, generously spaced, welcoming. Credible enough for teachers, calm enough that taking a quiz never feels stressful, and accessible to everyone.

### Compiler / namespace
An automated compiler bundles the components into `_ds_bundle.js` and indexes the tokens. In card / kit HTML, read components from **`window.QuiztinDesignSystem_138691`**. Consumers link the single root **`styles.css`**.

### Sources
This is a **from-scratch brand**, created from a written product brief. There was **no attached codebase, Figma file, or slide deck** — palette, type, radius, motion, and the component inventory were all proposed to fit the brief. If a real Quiztin codebase or Figma exists, share it and this system should be reconciled against it (it would become the source of truth).

---

## Fonts (please confirm)
Type is loaded from **Google Fonts** via `tokens/fonts.css` — the brand fonts are a *proposal*, not licensed/self-hosted files:
- **Fredoka** — display / brand / headings (rounded, friendly, characterful)
- **Nunito** — body & UI (warm, highly legible, rounded terminals)
- **Space Mono** — class join codes and code-like text

⚠️ **These are CDN webfonts (no local binaries shipped).** For production or offline use, we should self-host the licensed `.woff2` files and swap the `@import` in `tokens/fonts.css` for local `@font-face` rules. **If you'd prefer different type, tell me** — this is the easiest thing to change.

## Logo (absent by design)
No logo or mascot was provided, so **none was invented.** Wherever a mark would go, the brand name is set typographically ("Quiztin" in Fredoka with a coral dot). A coral **"Q" monogram** is used only as the AI companion's avatar — treat it as a placeholder. **If you have a real logo/mascot, send it** and we'll wire it in.

---

## CONTENT FUNDAMENTALS — how Quiztin talks

**Voice:** a warm, encouraging companion sitting beside the learner. Plain language, never clinical. Personable but credible — it never undermines a teacher's authority with goofiness.

- **Person:** speak *to* the user as **"you"**; the product/AI refers to itself as **"Quiztin"** or "we". ("You answered all 8 questions." / "Quiztin will walk you through anything you missed.")
- **Casing:** **Sentence case everywhere** — headings, buttons, labels, menus. No Title Case, no ALL-CAPS except tiny tracked eyebrow labels.
- **Tone by moment:**
  - *Taking a quiz* → calm, low-pressure. "Take the time you need. There's no timer."
  - *Results* → encouraging first, specific second. Frame misses as *"to review"*, never *"wrong/failed"*. "Nice — that's 7 out of 8. Let's look at the one you missed."
  - *AI feedback* → supportive, concrete, a little warm. Points at the idea, not the grade. "You're really close."
  - *Irreversible actions* → gentle confirmation, plain stakes. "Ready to submit? You can't change answers after submitting."
- **Numbers:** show scores as encouragement, always paired with a human line — never a bare "Score: 87.5%".
- **Emoji:** **not used.** Warmth comes from words, colour, and rounded form — we use Phosphor icons instead of emoji.
- **Avoid:** exam/testing jargon ("assessment", "incorrect response recorded"), warnings that raise anxiety ("Do not navigate away"), finality that feels punitive ("Submission is final.").

**Say / don't say**
- ✅ "Take the time you need." ❌ "Time remaining: 04:59."
- ✅ "Not quite — let's review this one." ❌ "Incorrect."
- ✅ "You can still change answers first." ❌ "Submission is final. Proceed?"

---

## VISUAL FOUNDATIONS

**Overall vibe.** Warm, soft, and calm. A cream canvas, warm-tinted neutrals, softly rounded everything, and gentle diffuse shadows that read like warm light. Colour is used sparingly and purposefully; the interface is mostly quiet so the content (and encouragement) stands out.

**Colour.**
- **Blueberry** (periwinkle-blue, `--primary` `#4453D6`) is the calm interactive workhorse — primary buttons, links, focus rings, selection, selected answers. It's deliberately calming for the low-anxiety take-quiz surface.
- **Coral** (warm apricot, `--accent` `#DD5530`; mark `#F26A41`) is the brand's warm signature — used sparingly for brand moments, encouragement, celebratory/warm actions, and as **the AI companion's voice** (Quiztin "speaks" in coral).
- **Sand** — warm-tinted neutrals from cream (`--sand-0 #FFFDFB`) to warm near-black ink (`--sand-900 #211C15`). Never cold grey.
- **Semantics are gentle:** success green, a soft **rose** for errors/incorrect (never an alarming pure red), honey amber for warnings. All text pairings target WCAG AA.
- See `tokens/colors.css` — base scales (50–900) plus semantic aliases (`--primary`, `--text-body`, `--surface-card`, `--answer-correct-border`, `--ai-surface`, …). Reach for the semantic aliases.

**Type.** Fredoka (display/brand/headings), Nunito (body/UI), Space Mono (codes). Display is set tight (`-0.02em`); body is generous (1.5–1.65 line-height) for comfortable reading. Scale runs 11 → 60px; UI text never below 14px, answer/question text is large (18–28px). Tokens in `tokens/typography.css`.

**Spacing.** 4px base grid but **generous by default** — reach for 16/24 before 8/12. Card padding 24, section rhythm 48–64, comfortable content max 1152px, reading measure 640px. Minimum touch target **44px**. Tokens in `tokens/spacing.css`.

**Corner radius.** Everything is softly rounded, nothing sharp: 14px fields/buttons, 20px cards, 28px answer tiles / dialogs, full **pills** for chips, badges, progress bars, and avatars. `tokens/radius.css`.

**Borders.** Hairline warm borders (`--border` = sand-200) on cards; slightly stronger sand-300 on fields and dividers. Selected/result states use a **2px** coloured border (blueberry/green/rose).

**Shadows / elevation.** Soft, diffuse, **warm-tinted** (brown-black, low alpha) — never pure black. Cards pair a hairline border **with** a gentle shadow (sm→md). Primary/accent buttons get a subtle *coloured* lift (`--shadow-primary` / `--shadow-accent`). Focus is a separate high-contrast blueberry glow, always visible. `tokens/shadow.css`.

**Backgrounds.** Flat warm cream (`--color-bg`), no busy textures or patterns. **Gradients are used sparingly** — only the teacher dashboard hero uses a blueberry gradient; the take-quiz surface stays flat and calm. No hand-drawn illustrations or photography are part of the core system (add real assets if the brand acquires them).

**Transparency & blur.** Used only for the modal backdrop — a warm translucent scrim with a light `backdrop-filter: blur`. Elsewhere surfaces are solid.

**Animation & motion.** Gentle and reassuring. Default is a quick **ease-out** (140–220ms); a soft **ease-spring** (no big overshoot) is reserved for encouraging/celebratory moments (checkmarks springing in, toggle thumbs, dialog pop). **Press = a calm shrink** (`scale .97`); **hover = a −2px lift** or a soft colour shift. Progress bars ease slowly so they never feel jumpy. **All motion respects `prefers-reduced-motion`.** `tokens/motion.css`.

**Hover / press states.**
- Buttons: hover darkens the fill (primary → primary-hover); press shrinks slightly.
- Secondary/ghost: hover adds a soft sand or tinted wash.
- Cards (interactive): hover lifts and deepens the shadow; press barely shrinks.
- Answer tiles: hover warms the border + faint sand fill; selection is a blueberry border + tint.

**Cards.** White surface, 20px radius, hairline border **plus** a soft shadow, 24px padding. The single most-used surface — classrooms, quizzes, results, settings all sit on cards. Never a coloured left-border-only card (an anti-pattern we avoid).

**Imagery vibe.** No stock imagery in the core system. If added, keep it warm, bright, and human — matching the cream/coral palette rather than cool or high-contrast.

---

## ICONOGRAPHY

- **System:** **Phosphor Icons** (web font) — rounded, friendly line icons that match Quiztin's soft forms. Loaded from CDN (`@phosphor-icons/web`), weights **regular** / **bold** / **fill**.
- ⚠️ **Substitution flag:** Phosphor is a *chosen* icon set for this from-scratch brand (there was no source icon set to copy). If Quiztin has its own icons, swap them in and update the `Icon` component + this section.
- **Component:** use the **`Icon`** wrapper (`<Icon name="check-circle" weight="fill" />`) rather than raw `<i>` where possible; it standardises sizing and accessibility (decorative by default, `label` when meaningful).
- **Weight usage:** `regular` for idle UI, `fill` for selected/active/emphasis states, `bold` for small glyphs inside buttons.
- **No hand-drawn SVG icons.** Don't reconstruct icons by hand — use Phosphor names. Common glyphs: `house`, `chalkboard-teacher`, `student`, `users-three`, `list-checks`, `sparkle`, `check-circle`, `x-circle`, `clock`, `pencil-simple`, `plus`, `caret-right`, `trophy`, `paper-plane-tilt`, `chat-teardrop-dots`.
- **Emoji / unicode as icons:** not used. (A few unicode marks like ✓/✕ appear inside foundation *specimen cards* for illustration only — product UI uses Phosphor via `Icon`.)

---

## Components

Reusable React primitives, grouped by concern. Each is `Name.jsx` + `Name.d.ts` + `Name.prompt.md`, exported under `window.QuiztinDesignSystem_138691`. Every directory has a `@dsCard` for the Design System tab.

**Foundation** — `Icon`
**Actions** (`components/actions/`) — `Button`, `IconButton`
**Forms** (`components/forms/`) — `TextField`, `Select`, `Checkbox`, `Radio`, `Switch`
**Feedback** (`components/feedback/`) — `Badge`, `ProgressBar`, `Toast`, `Tooltip`
**Surfaces** (`components/surfaces/`) — `Card`, `Dialog`
**Navigation** (`components/navigation/`) — `Tabs`
**Quiz** (`components/quiz/`) — `AnswerChoice`, `AIFeedbackCard`, `ResultSummary`

### Intentional additions
Because no source component library was provided, a standard primitive set was authored, plus these **domain components** that are core to Quiztin's job (each earns its place):
- **`AnswerChoice`** — the selectable answer tile; the one component that carries both *answering* and *reviewing* (idle/selected → correct/incorrect/missed).
- **`AIFeedbackCard`** — the distinct, supportive AI companion voice (explicitly required to read as a helpful voice, not a grade stamp).
- **`ResultSummary`** — the encouraging post-submit score card (headline + calm ring + counts framed as "to review").
- **`ProgressBar`** — elevated to a core component because calm, ever-present progress is a non-negotiable of the take-quiz flow.
- **`Icon`** — a thin Phosphor wrapper for consistent, accessible glyph usage.

---

## UI kits
High-fidelity, interactive click-throughs of the two product surfaces (recreations built from the components above, data mocked):
- **`ui_kits/teacher/`** — Dashboard → Quiz editor → Class results (with a publish-confirm Dialog + success Toast). See its `README.md`.
- **`ui_kits/student/`** — Home → calm Take-quiz flow → Results review with AI feedback. See its `README.md`.

---

## Starting points
Marked for the consumer "Starting Points" picker: `Button`, `Card`, `ProgressBar`, `AnswerChoice`, `AIFeedbackCard`, `ResultSummary` (via `@startingPoint` in their `.d.ts`).

---

## Index / manifest (root)
- `styles.css` — global entry point (@import manifest; link this one file)
- `tokens/` — `fonts.css`, `colors.css`, `typography.css`, `spacing.css`, `radius.css`, `shadow.css`, `motion.css`, `base.css`
- `guidelines/` — foundation specimen cards (Colors, Type, Spacing, Radius, Elevation, Motion, Brand)
- `components/<group>/` — the primitives above (`.jsx` + `.d.ts` + `.prompt.md` + a `.card.html`)
- `ui_kits/teacher/`, `ui_kits/student/` — product recreations
- `SKILL.md` — makes this system usable as a downloadable Agent Skill
- `_ds_bundle.js`, `_ds_manifest.json`, `_adherence.oxlintrc.json` — generated by the compiler (do not edit)
