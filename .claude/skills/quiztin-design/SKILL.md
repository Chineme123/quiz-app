---
name: quiztin-design
description: Use this skill to generate well-branded interfaces and assets for Quiztin, either for production or throwaway prototypes/mocks/etc. Contains essential design guidelines, colors, type, fonts, and the token system.
user-invocable: true
---

Read `design-system/readme.md` at the repo root first — it is the full design guide. Then read `design-system/HANDOFF.md` for per-component prop specs.

If creating visual artifacts (slides, mocks, throwaway prototypes), copy assets out and create static HTML files for the user to view. If working on production code, read the rules here and in `readme.md` to become an expert in designing with this brand.

If the user invokes this skill without any other guidance, ask them what they want to build or design, ask some questions, and act as an expert designer who outputs HTML artifacts _or_ production code, depending on the need.

## Quiztin in one paragraph
A classroom quiz web app that should feel like a **helpful, encouraging companion** — not a testing tool. Warm, rounded, generously spaced, soft welcoming colour. Taking a quiz stays calm and low-anxiety; AI feedback ("Quiztin") reads as a supportive voice, never a grade stamp; everything is accessible (high contrast, keyboard, screen-reader).

## Where things are

Everything lives at the repo root under `design-system/`. This skill points at those files rather than copying them, so there is one source of truth.

- `design-system/readme.md` — the full design guide: content voice, visual foundations, iconography. **Read this first.**
- `design-system/HANDOFF.md` — per-component prop specs, states, and a11y requirements. **The contract for building a component.**
- `design-system/styles.css` — link this one file to get all tokens + base styles.
- `design-system/tokens/` — colours, type, spacing, radius, shadow, motion, fonts. `base.css` is the reset, the focus ring, and the reduced-motion rules.
- `design-system/_ds_bundle.js` — a compiled browser global (`window.QuiztinDesignSystem_138691`). Useful for standalone HTML artifacts, and as a **reference** for markup, `qz-` class names, and state handling.

Also read `context/ui-rules.md` (the law: prime directive, per-screen rules) and `context/ui-registry.md` (which components exist vs are planned).

## There is no React source

The export ships **no importable React components**. `_ds_bundle.js` is a Babel-compiled browser global with no imports or exports; a bundler cannot meaningfully consume it. Production components are **authored by hand** in `frontend/src/components/ui/`, built from `HANDOFF.md` with `_ds_bundle.js` read as a reference. Do not try to import the bundle.

## Quick-start rules
- **Colour:** Blueberry (`--primary`) for calm interactive elements; Coral (`--accent`) for warm brand moments + the AI voice; warm Sand neutrals on a cream canvas; gentle green/rose/amber semantics. Use semantic tokens (`--text-body`, `--surface-card`, `--answer-correct-border`, `--ai-surface`, …). **Never a raw hex.**
- **Type:** Fredoka (display/headings/brand), Nunito (body/UI), Space Mono (join codes). Sentence case everywhere.
- **Shape & depth:** softly rounded (14 fields, 20 cards, 28 tiles, pills for chips); soft warm shadows + hairline borders; always-visible blueberry focus ring; 44px min touch targets.
- **Motion:** gentle ease-out; soft spring for encouraging moments; press = shrink .97; respect `prefers-reduced-motion`.
- **Voice:** warm, plain, "you"-focused, encouraging. Frame misses as "to review". No emoji. Confirm before irreversible actions.
- **Icons:** Phosphor (regular/bold/fill). In the app, `@phosphor-icons/react` via the `Icon` component. No hand-drawn SVG, no emoji.

## In production code (the `frontend/` app)

Per spec `docs/specs/0001-frontend-foundation/`, Tailwind v4 is bound to these tokens by reference:

```css
@theme inline { --color-primary: var(--primary); /* semantic aliases only */ }
```

So `bg-primary` compiles to `var(--primary)`. Use utilities for layout; use the token variables directly in component CSS. Tailwind **Preflight is not loaded** (`base.css` is already the reset) — which means `border-style: solid` must be restored in an `@layer base` rule, or `border` utilities render invisible.

## Using components in a standalone HTML artifact

Link `styles.css`, load the Phosphor web font and React + Babel, load the compiled `_ds_bundle.js`, then read components from the namespace:

```html
<link rel="stylesheet" href="design-system/styles.css">
<link rel="stylesheet" href="https://unpkg.com/@phosphor-icons/web@2.1.1/src/regular/style.css">
<link rel="stylesheet" href="https://unpkg.com/@phosphor-icons/web@2.1.1/src/bold/style.css">
<link rel="stylesheet" href="https://unpkg.com/@phosphor-icons/web@2.1.1/src/fill/style.css">
<!-- React 18 + Babel standalone, then: -->
<script src="design-system/_ds_bundle.js"></script>
<script type="text/babel">
  const { Button, Card, AnswerChoice, AIFeedbackCard } = window.QuiztinDesignSystem_138691;
  // …render…
</script>
```

## Caveats to surface to the user
- Fonts are **Google Fonts CDN** (Fredoka / Nunito / Space Mono), not self-hosted licensed files.
- There is **no logo/mascot** — the brand name is set typographically; a coral "Q" is a placeholder AI avatar.
- There is **no dark mode** in the export. Decide before component count grows.
- This brand was created **from a written brief** (no source codebase/Figma). If a real one exists, reconcile against it.
