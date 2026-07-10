---
name: quiztin-design
description: Use this skill to generate well-branded interfaces and assets for Quiztin, either for production or throwaway prototypes/mocks/etc. Contains essential design guidelines, colors, type, fonts, assets, and UI kit components for prototyping.
user-invocable: true
---

Read the README.md file within this skill, and explore the other available files.

If creating visual artifacts (slides, mocks, throwaway prototypes, etc), copy assets out and create static HTML files for the user to view. If working on production code, you can copy assets and read the rules here to become an expert in designing with this brand.

If the user invokes this skill without any other guidance, ask them what they want to build or design, ask some questions, and act as an expert designer who outputs HTML artifacts _or_ production code, depending on the need.

## Quiztin in one paragraph
A classroom quiz web app that should feel like a **helpful, encouraging companion** — not a testing tool. Warm, rounded, generously spaced, soft welcoming colour. Taking a quiz stays calm and low-anxiety; AI feedback ("Quiztin") reads as a supportive voice, never a grade stamp; everything is accessible (high contrast, keyboard, screen-reader).

## Where things are
- `readme.md` — the full design guide: content voice, visual foundations, iconography, component + UI-kit index. **Read this first.**
- `styles.css` — link this one file to get all tokens + base styles.
- `tokens/` — colours, type, spacing, radius, shadow, motion, fonts.
- `guidelines/` — foundation specimen cards (colours, type, spacing, etc.).
- `components/<group>/` — reusable React primitives (`.jsx` + `.d.ts` + `.prompt.md`).
- `ui_kits/teacher/`, `ui_kits/student/` — full interactive screen recreations to learn layout patterns from.

## Quick-start rules
- **Colour:** Blueberry (`--primary`) for calm interactive elements; Coral (`--accent`) for warm brand moments + the AI voice; warm Sand neutrals on a cream canvas; gentle green/rose/amber semantics. Use semantic tokens (`--text-body`, `--surface-card`, `--answer-correct-border`, `--ai-surface`, …).
- **Type:** Fredoka (display/headings/brand), Nunito (body/UI), Space Mono (join codes). Sentence case everywhere.
- **Shape & depth:** softly rounded (14 fields, 20 cards, 28 tiles, pills for chips); soft warm shadows + hairline borders; always-visible blueberry focus ring; 44px min touch targets.
- **Motion:** gentle ease-out; soft spring for encouraging moments; press = shrink .97; respect `prefers-reduced-motion`.
- **Voice:** warm, plain, "you"-focused, encouraging. Frame misses as "to review". No emoji. Confirm before irreversible actions.
- **Icons:** Phosphor (regular/bold/fill) via the `Icon` component. No hand-drawn SVG, no emoji.

## Using components in a standalone HTML artifact
Link `styles.css`, load the Phosphor web font and React + Babel, load the compiled `_ds_bundle.js`, then read components from the namespace:

```html
<link rel="stylesheet" href="styles.css">
<link rel="stylesheet" href="https://unpkg.com/@phosphor-icons/web@2.1.1/src/regular/style.css">
<link rel="stylesheet" href="https://unpkg.com/@phosphor-icons/web@2.1.1/src/bold/style.css">
<link rel="stylesheet" href="https://unpkg.com/@phosphor-icons/web@2.1.1/src/fill/style.css">
<!-- React 18 + Babel standalone, then: -->
<script src="_ds_bundle.js"></script>
<script type="text/babel">
  const { Button, Card, AnswerChoice, AIFeedbackCard } = window.QuiztinDesignSystem_138691;
  // …render…
</script>
```

(See any `components/*/*.card.html` or a `ui_kits/*/index.html` for a complete working example.)

## Caveats to surface to the user
- Fonts are **Google Fonts CDN** (Fredoka / Nunito / Space Mono), not self-hosted licensed files.
- There is **no logo/mascot** — the brand name is set typographically; a coral "Q" is a placeholder AI avatar.
- This brand was created **from a written brief** (no source codebase/Figma). If a real one exists, reconcile against it.
