# Quiztin — UI Tokens

> Generated 2026-07-09 from the Claude Design export committed at **`design-system/`**. The **source of truth for token values is `design-system/tokens/*.css`** — this file documents the architecture, the semantic contract, and the invariant. For *why* the brand looks/sounds this way, see `foundation.md` §1 (brand voice); for how tokens compose into UI, see `ui-rules.md`.

**Status:** ✅ generated from the design export (was PENDING).

## The layered architecture
**raw palette (private scales) → semantic aliases (the contract) → framework binding.**
- **The invariant:** components reference **semantic aliases only** (`var(--primary)`, `var(--text-body)`, `var(--answer-correct-border)`…) — **never a raw hex, never an off-palette value, and never a raw scale step** (`--blueberry-600`) directly. Raw scales exist so the aliases have something to point at; UI code points at the aliases.

## Framework binding
- **Single entry point:** `design-system/styles.css` — an `@import` manifest (order: fonts → colors → typography → spacing → radius → shadow → motion → base). Link/import this one file and you get every token plus a light friendly reset.
- All tokens are **CSS custom properties on `:root`** (vanilla CSS — there is **no Tailwind** in the export).
- **In our frontend (React + Vite):** import `design-system/styles.css` at the app root; components style against the semantic CSS vars. If Tailwind is later adopted (styling is still open in `library-docs.md`), **bind the semantic vars into Tailwind `@theme`** — don't duplicate the values.
- **Theming:** the export is **light-only** (warm cream canvas). 🕗 **Dark mode is not in the export** — decide if Quiztin needs it; if so, request/derive a dark palette and add a `data-theme` switch (don't hand-invent one).

## Color — `design-system/tokens/colors.css`
**Raw palette (private — do not use directly):**
- **Sand** (warm neutrals, never cold grey): `--sand-0 #FFFDFB` … `--sand-900 #211C15`
- **Blueberry** (calm primary): `--blueberry-50 #EEF1FE` … `--blueberry-600 #4453D6` (primary) … `--blueberry-900`
- **Coral** (warm brand + AI voice): `--coral-500 #F26A41` (brand mark) · `--coral-600 #DD5530` (accent)
- **Gentle semantics:** green (success), **rose** (incorrect — soft, never a pure alarming red), amber (warning)

**Semantic aliases (THE CONTRACT — reach for these):**
- **Surfaces:** `--color-bg` (cream) · `--surface-card` (`#FFF`) · `--surface-sunken` · `--surface-inverse`
- **Text:** `--text-strong` / `-body` / `-muted` / `-subtle` / `-disabled` · `--text-on-primary` · `--text-link`
- **Borders:** `--border` (sand-200) · `--border-field` (sand-300) · `--divider`
- **Primary (Blueberry):** `--primary` / `-hover` / `-press` / `-soft` / `-softer` / `-text`
- **Accent (Coral — sparingly):** `--accent` / `-hover` / `-soft` / `-text` · `--brand-mark`
- **Feedback:** `--success` / `--danger` / `--warning` (+ `-soft` / `-border` / `-text`)
- **Quiz answer states:** `--answer-idle-border` · `--answer-selected-bg/-border` · `--answer-correct-bg/-border` · `--answer-incorrect-bg/-border`
- **AI companion:** `--ai-surface` / `-border` / `-accent` / `-text` (coral apricot — Quiztin "speaks" in coral, visually distinct from blue chrome)
- **Focus:** `--focus-ring` · `--focus-ring-shadow` (always-visible blueberry glow)
- All text pairings target **WCAG AA**.

## Type — `tokens/typography.css` + `tokens/fonts.css`
- **Families:** `--font-display` Fredoka · `--font-body` Nunito · `--font-mono` Space Mono (join codes).
- **Scale:** `--text-2xs` 11 → `--text-base` 16 → `--text-5xl` 60. UI text never < 14px; question/answer text is large (18–28).
- **Weights** 400–900 · **leading** `none`→`relaxed` (1–1.65) · **tracking** tight→code.
- **Semantic type roles (use these composites):** `--type-display/-hero/-title/-heading/-subheading/-question/-body(-lg/-strong)/-label/-caption/-fine`.

## Spacing — `tokens/spacing.css` (4px grid, **generous by default**)
- `--space-0` … `--space-12` (0 → 96px). Default gap 16 (`--space-4`); card padding 24 (`--space-6`); section rhythm 48–64.
- **Semantic:** `--gutter` · `--stack` · `--field-gap` · `--section-gap` · `--content-max` 1152 · `--reading-max` 640 · `--tap-min` **44px** (a11y).

## Radius — `tokens/radius.css` (soft, nothing sharp)
- `--radius-xs` 6 … `--radius-md` 14 (buttons/fields) · `--radius-lg` 20 (cards) · `--radius-xl` 28 (tiles/dialogs) · `--radius-pill` 999.
- **Semantic:** `--radius-button/-input/-card/-tile/-modal/-chip`.

## Elevation — `tokens/shadow.css` (soft, **warm-tinted** — brown-black, never pure black)
- `--shadow-xs`…`-xl` · coloured lifts `--shadow-primary` / `--shadow-accent` · `--shadow-inset` · focus ring is separate.
- Cards pair a **hairline border WITH a soft shadow**.

## Motion — `tokens/motion.css` (gentle, reassuring)
- Durations `--duration-instant` 80 … `--duration-slower` 520 · `--ease-out` (default) · `--ease-spring` (soft encouraging, no big overshoot).
- `--press-scale` .97 (calm shrink) · `--lift-y` -2px (hover lift) · `--transition-colors/-base`. **All motion respects `prefers-reduced-motion`** (handled in `base.css`).

## Icons
- **Phosphor Icons** (web font, CDN) via an `Icon` wrapper. Regular = idle, fill = active/selected, bold = inside buttons.

## 🕗 Confirm / TBD (the export flags these as proposals for a from-scratch brand)
- **Fonts** — Fredoka/Nunito/Space Mono via Google Fonts CDN; confirm the choice and **self-host licensed `.woff2`** for production.
- **Logo/mascot** — none provided; the brand is set typographically ("Quiztin" in Fredoka with a coral dot); a coral **"Q" monogram** is the AI-avatar placeholder. Wire in a real mark if one exists.
- **Icons** — Phosphor is a *chosen* set; swap if Quiztin has its own.
- **Dark mode** — not in the export (see Theming above).
