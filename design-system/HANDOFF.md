# Handoff: Quiztin Design System

## Overview
**Quiztin** is a classroom quiz web app that feels like a helpful, encouraging *companion* — not a bureaucratic testing tool. Teachers author and publish quizzes inside classrooms; students take them, are scored immediately, and review results question-by-question with supportive **AI feedback** ("Quiztin", a friendly voice). This bundle is the complete design system — visual foundations (tokens), reusable UI components, and two full product surfaces (teacher + student) — everything needed to build Quiztin's UI on-brand.

## About the design files
The files in this bundle are **design references authored in HTML/CSS/JSX** — prototypes that show the intended look, behavior, and voice. **They are not production code to copy verbatim.** Your task is to **recreate these designs in the target codebase** (Quiztin is a React web app) using its established patterns, routing, data layer, and libraries. If no front-end exists yet, React + CSS custom properties is the natural choice (that's how the reference is structured).

The components are plain React + CSS custom properties (no CSS-in-JS lib, no UI framework). You can lift them almost directly, or map them onto your component library — but reproduce the **exact token values, spacing, radii, and states** below.

## Fidelity: **High-fidelity**
Final colors, typography, spacing, radii, motion, and interaction states. Recreate pixel-accurately. Every numeric value here is intentional — do **not** snap to a different 4/8px grid or a framework default.

---

## Design tokens
All tokens are CSS custom properties (see `design-system/tokens/*.css`). Reach for the **semantic** aliases in product code, not the raw scales.

### Color
**Primary — Blueberry** (calm periwinkle-blue; interactive workhorse: buttons, links, focus, selection, selected answers)
`50 #EEF1FE · 100 #DEE4FD · 200 #C2CCFB · 300 #9CACF7 · 400 #7385F0 · 500 #5566E6 · 600 #4453D6 (primary) · 700 #3742B0 (hover) · 800 #2C3488 (press) · 900 #1F2560`

**Accent — Coral** (warm brand voice + the AI companion; used sparingly)
`50 #FFF4EF · 100 #FFE6DC · 200 #FFCBB6 · 300 #FFA98A · 400 #FB855E · 500 #F26A41 (mark) · 600 #DD5530 (accent) · 700 #B7401F (hover) · 800 #8F3117 · 900 #5F2210`

**Neutrals — Sand** (warm-tinted, on a cream canvas — never cold grey)
`0 #FFFDFB · 50 #FBF7F2 · 100 #F4EEE6 · 200 #E9E0D5 · 300 #D8CCBD · 400 #B9AB99 · 500 #948671 · 600 #6F6353 · 700 #4F4638 · 800 #352E24 · 900 #211C15 (ink)`

**Semantics (gentle)** — Success `#1E8A57` / soft `#E9F7EF`; Danger (rose, never alarming red) `#C8404E` / soft `#FDEEF0`; Warning (honey) `#BE7712` / soft `#FDF3E2`.

**Key semantic aliases:** `--color-bg` = sand-50 (cream canvas) · `--surface-card` = #FFFFFF · `--text-strong` = sand-900 · `--text-body` = sand-800 · `--text-muted` = sand-600 · `--border` = sand-200 · `--border-field` = sand-300 · `--primary` = blueberry-600 · `--accent` = coral-600 · `--ai-surface` = coral-50 · `--ai-border` = coral-200 · `--ai-accent` = coral-600. Answer states: `--answer-selected-border` = blueberry-600, `--answer-correct-border` = green-600, `--answer-incorrect-border` = rose-600. All text/background pairings target **WCAG AA**.

### Typography
- **Fredoka** — display, headings, brand. Weights 400/500/600/700. Set tight (`letter-spacing: -0.02em` on large display).
- **Nunito** — body & UI. Weights 400/500/600/700/800.
- **Space Mono** — class join codes / code-like text (uppercased, `letter-spacing: 0.08–0.14em`).
- **Scale (px):** 11, 12, 14, 16 (base), 18, 22, 28, 36, 48, 60.
- **Line-height:** display 1.15, headings 1.3, UI 1.5, long-form/AI 1.65.
- UI text never below 14px; question/answer text is large (18–28px). Sentence case everywhere; ALL-CAPS only for tiny tracked eyebrow labels.

### Spacing (4px base, generous by default)
`4, 8, 12, 16, 24, 32, 40, 48, 64, 80, 96`. Card padding **24**, section rhythm **48–64**, content max **1152px**, reading measure **640px**. **Minimum touch target 44px.**

### Radius
`sm 10 · md 14 (buttons/fields) · lg 20 (cards) · xl 28 (answer tiles/dialogs) · 2xl 36 · pill 999 (chips, badges, progress, avatars)`. Everything is softly rounded; nothing sharp.

### Shadow / elevation (soft, WARM-tinted — never pure black)
- `xs 0 1px 2px rgba(53,46,36,.06)` · `sm 0 2px 6px rgba(53,46,36,.08)` · `md 0 6px 16px -2px rgba(53,46,36,.12)` · `lg 0 14px 34px -8px rgba(53,46,36,.16)` · `xl 0 28px 64px -12px rgba(53,46,36,.22)`.
- Coloured lifts: `--shadow-primary 0 8px 20px -6px rgba(68,83,214,.45)`, `--shadow-accent 0 8px 20px -6px rgba(221,85,48,.42)`.
- **Cards pair a hairline border WITH a soft shadow.** Never a coloured-left-border-only card.
- **Focus ring** (separate, always visible): `box-shadow: 0 0 0 4px rgba(85,102,230,.32)`.

### Motion
- Durations: fast 140ms, base 220ms, slow 340ms.
- Easing: default ease-out `cubic-bezier(.22,.75,.28,1)`; soft spring (encouraging moments) `cubic-bezier(.34,1.4,.5,1)` — no big overshoot.
- **Press = `scale(.97)`; hover = `translateY(-2px)`** or a soft color shift. Progress bars ease slowly (never jumpy).
- **Respect `prefers-reduced-motion: reduce`** everywhere.

---

## Components
Full implementations are in `design-system/_ds_bundle.js` (readable JS, one function per component); every prop/spec is documented below. Raw per-file sources — `Name.jsx` + `Name.d.ts` + `Name.prompt.md` — live in the Quiztin design-system project.

- **Icon** (`foundation/`) — Phosphor web-font wrapper. `name`, `weight` (regular/bold/fill), `size`, `label`. Decorative (aria-hidden) unless `label` given.
- **Button** (`actions/`) — variants `primary` (blueberry) / `accent` (coral) / `secondary` / `subtle` / `ghost` / `danger`; sizes `sm|md|lg`; `icon`/`iconRight`, `loading`, `fullWidth`. Radius 14, one primary per view, press-shrink, focus ring, 44px+ target.
- **IconButton** (`actions/`) — square icon-only; requires `label`; variants ghost/secondary/primary/accent/danger; sizes 34/44/52.
- **TextField** (`forms/`) — `label`, `hint`, `error` (gentle rose, sets aria-invalid), `icon`, `multiline`, `required`/`optional`. 1.5px border, focus = blueberry ring.
- **Select** (`forms/`) — styled native select + caret; `options` or `<option>` children; `placeholder`.
- **Checkbox / Radio / Switch** (`forms/`) — hidden native input + styled control; `label` + `description`; check springs in; Switch is `role="switch"` for instant settings.
- **Badge** (`feedback/`) — pill; tones neutral/primary/accent/success/danger/warning; `sm` (uppercase) / `md`; `solid`, `dot`, `icon`. Also the tag primitive.
- **ProgressBar** (`feedback/`) — calm progress; `value`/`max`, `label`, `showCount` + `countFormat` (e.g. "Question 3 of 8"); tones primary/accent/success; `role="progressbar"`. **Backbone of the low-anxiety quiz flow.**
- **Toast** (`feedback/`) — notification/inline alert; tones info/success/danger/warning/**ai** (warm apricot); `title`, message, `icon`, `onClose`; role status/alert.
- **Tooltip** (`feedback/`) — dark bubble on hover **and** focus; `content`, `placement` top/bottom; links via aria-describedby.
- **Card** (`surfaces/`) — the workhorse surface; `variant` raised/flat/sunken; `padding` none/sm/md/lg; `interactive` → focusable hover-lifting button/link. White, 20px radius, hairline border + soft shadow.
- **Dialog** (`surfaces/`) — accessible modal; Escape/backdrop/close all call `onClose`; `title`, `description`, `icon`, `tone`, `footer`; scroll-lock; role dialog + aria-modal. **Use for the calm confirm-before-submit.**
- **Tabs** (`navigation/`) — roving-focus keyboard (←/→/Home/End); `underline` (sections) / `pill` (segmented); controlled or uncontrolled; you render the panel.
- **AnswerChoice** (`quiz/`) — the selectable answer tile; states `idle` / `selected` (blueberry) / `correct` (green ✓) / `incorrect` (rose ✕, student's wrong pick) / `missed` (dashed green, right answer not chosen). Big touch target; result states auto-disable. **Carries both answering and reviewing.**
- **AIFeedbackCard** (`quiz/`) — Quiztin's AI voice; warm apricot surface + coral "Q" avatar + "AI feedback" pill; `<em>` = coral highlight; `loading` shimmer. Supportive companion, never a grade stamp.
- **ResultSummary** (`quiz/`) — post-submit score card; `correct`/`total`; auto encouraging headline + ring color by score; wrong answers framed as "to review".

---

## Screens / Views
Both surfaces are specified below; the interactive recreations live in the Quiztin design-system project's `ui_kits/` (teacher + student). Rebuild them in your app from these specs and the component list above.

### Teacher (`ui_kits/teacher/`)
1. **Dashboard** — gradient greeting hero + stat chips; "Your classes" grid of Cards (color dot, name, mono join code, student count); "Quizzes" list of Card rows (icon tile, title, class·N questions, Live/Draft Badge, class-average number, Edit/Results Buttons). Sidebar nav (Home/Quizzes/Results/Classes) + teacher card.
2. **Quiz editor** — two columns. Main: editable question Cards (tap a circle to set the correct answer, editable prompt + option inputs, add answer/question), an inline AIFeedbackCard hint. Side: quiz details (title, class) + option Switches. Topbar Publish (accent) → **confirm Dialog** → Results + success Toast.
3. **Quiz results** — class-average ring + stats; AIFeedbackCard class insight; per-question breakdown bars (color by % correct, low ones flagged "worth revisiting"); filterable student list (pill Tabs) with score pills.

### Student (`ui_kits/student/`)
1. **Home** — greeting; "To do" quiz Cards with due Badges + Start; join-a-class field; recently-completed rows with score pills + Review.
2. **Take quiz** — the calm core. Slim top bar; persistent **ProgressBar** ("Question X of Y") + jumpable numbered dots; one big question; large **AnswerChoice** tiles; "no timer, take your time" reassurance; sticky Back / Next footer; on the last question **Review & submit** opens a **confirm Dialog** ("Ready to submit? You can't change answers after submitting").
3. **Review** — encouraging **ResultSummary** + overall **AIFeedbackCard**; question-by-question: each answer shows its state (correct / your answer / correct answer); missed questions get a supportive AI note.

Two starting-point layouts exist in the project's `templates/`: `take-quiz` (calm quiz + results) and `teacher-home` (dashboard).

---

## Interactions & behavior
- **Confirm before irreversible actions** (submit quiz, publish, delete) via Dialog — non-negotiable.
- **Take-quiz:** answers changeable until submit; progress always visible; jump between questions via dots; no timer.
- **Publish flow (teacher):** Publish → confirm Dialog → navigate to Results + success Toast (auto-dismiss ~3.6s).
- **Motion:** ease-out for most transitions; soft spring for check-ins, toggle thumbs, dialog pop; press-shrink on buttons/tiles/cards; slow eased progress fill. Honor reduced-motion.
- **Hover/press:** buttons darken + press-shrink; secondary/ghost get a soft wash; interactive cards lift + deepen shadow; answer tiles warm the border + faint fill, selection = blueberry border + tint.

## State management (per surface)
- **Take quiz:** `currentIndex`, `answers: {questionIndex → optionId}`, `submitted`. Score = count of `answers[i] === question.correct`.
- **Editor:** `questions[]` (prompt, options[], correctId), `settings` (shuffle/showAnswers/retakes), `published`.
- **Teacher app:** current screen + active quiz; publish-confirm open; transient toast.
- **Student app:** current screen (home/quiz/review) + last result. Real app: fetch classes, assigned/completed quizzes, questions, submissions; AI feedback from the results service.

## Accessibility (non-negotiable)
High-contrast AA text; always-visible blueberry focus ring on every interactive element; full keyboard support (Tabs roving focus, Dialog Escape + focus management, native form controls); `role="progressbar"`/`status`/`alert`/`switch` where relevant; 44px min targets; decorative icons aria-hidden, meaningful ones labelled; `prefers-reduced-motion` respected.

## Voice & tone
Warm, plain, encouraging; address the user as "you", the product as "Quiztin". Frame misses as "to review", never "wrong/failed". No emoji (warmth via words + color + form). AI feedback is specific and supportive. Confirmations state stakes plainly and kindly. See `design-system/readme.md` → "Content Fundamentals" for say/don't-say examples.

## Iconography
**Phosphor Icons** (web font: `@phosphor-icons/web`, weights regular/bold/fill) via the `Icon` component. `regular` idle, `fill` selected/active, `bold` small glyphs. No hand-drawn SVG icons, no emoji.

## Assets
- **Fonts:** Fredoka, Nunito, Space Mono — currently **Google Fonts CDN** (see `design-system/tokens/fonts.css`); for production, self-host licensed `.woff2` and replace the `@import` with local `@font-face`.
- **Logo:** none provided — the wordmark is set typographically ("Quiztin" in Fredoka + coral dot); the coral "Q" is a placeholder AI avatar. Swap in a real mark when available.
- **Icons:** Phosphor (CDN or npm `@phosphor-icons/react` in a React app).

## Files (in this bundle, under `design-system/`)
- `styles.css` — global entry point (@import manifest; link this one file).
- `tokens/` — `fonts, colors, typography, spacing, radius, shadow, motion, base` .css: all design tokens as CSS custom properties.
- `_ds_bundle.js` — compiled component library: every component's implementation as readable JS (one function each). Use it to read exact markup / class structure while you rebuild in your framework.
- `readme.md` — the full Quiztin design guide (voice, visual foundations, iconography, component index).
- `SKILL.md` — the design system as an invocable skill.

Raw per-component sources (`components/<group>/Name.jsx` + `.d.ts` + `.prompt.md`), the interactive UI kits (`ui_kits/teacher`, `ui_kits/student`), the starting-point templates (`templates/`), and foundation specimen cards (`guidelines/`) all live in the **Quiztin design-system project** — reference them there. They're intentionally omitted here so this bundle stays a clean, framework-agnostic spec (and doesn't shadow your app's own components).

## Implementation notes
- Ship the token CSS (`styles.css` + `tokens/`) as global CSS custom properties; every component reads them, so theming/adjustments happen in one place.
- Components are React + inline `<style>` injection keyed to tokens — port them to your styling approach but keep the class/state structure and exact values.
- The HTML kits load React + Babel + `_ds_bundle.js` from CDN for the prototype; a real app compiles the `.jsx` normally.
