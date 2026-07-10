# Quiztin — UI Rules

> How tokens compose into UI. Generated 2026-07-09 from the Claude Design export (`design-system/`). Tokens live in `ui-tokens.md`; *why* the brand is this way lives in `foundation.md` §1.

**Status:** ✅ generated from the design export (was PENDING).

## §0 Prime directive
Every screen serves one sentence: **Quiztin is a helpful, encouraging companion — not a bureaucratic testing tool.** Warm, calm, generously spaced, and accessible; credible enough for teachers, calm enough that taking a quiz never feels stressful, welcoming to everyone. When a call is ambiguous, choose the option that feels more like a supportive companion.

## §1 Voice & content (how Quiztin talks)
- Address the user as **"you"**; the product/AI is **"Quiztin"** / "we".
- **Sentence case everywhere** — headings, buttons, labels, menus. No Title Case; no ALL-CAPS except the tiny tracked eyebrow label.
- **Tone by moment:** *taking a quiz* → calm, low-pressure; *results* → encouraging first, specific second, framing misses as **"to review"** (never "wrong/failed"); *AI feedback* → supportive, points at the idea not the grade; *irreversible actions* → gentle confirmation with plain stakes ("Ready to submit? You can't change answers after submitting.").
- Scores always paired with a human line — never a bare "Score: 87.5%".
- **No emoji.** Warmth comes from words, colour, and rounded form (+ Phosphor icons).
- **Avoid:** exam/testing jargon, anxiety-raising warnings ("Do not navigate away"), punitive finality ("Submission is final.").
- Say / don't: ✅ "Take the time you need." ❌ "Time remaining: 04:59." · ✅ "Not quite — let's review this one." ❌ "Incorrect."

## §2 Layout & density
Generous by default — reach for 16/24 (`--space-4/-6`) before 8/12. Card padding 24; section rhythm 48–64; content max **1152**, reading measure **640**. Minimum touch target **44px**. Flat warm cream backgrounds, no busy textures.

## §3 Hierarchy & type
Fredoka (`--font-display`) for headings and brand moments; Nunito (`--font-body`) for everything read. Question and answer text is **large** (18–28); UI text never below 14px. Use the semantic `--type-*` roles, not ad-hoc size+weight.

## §4 Colour discipline
- **Blueberry** is the calm interactive workhorse — primary buttons, links, focus, selection, selected answers (deliberately calming for the take-quiz surface).
- **Coral** is the warm signature — used **sparingly** for brand moments, encouragement, and as **the AI companion's voice** (Quiztin speaks in coral).
- **Sand** neutrals only (never cold grey). Semantics are gentle — soft **rose** for incorrect, never an alarming pure red.
- Reach for **semantic aliases** (`ui-tokens.md`); everything targets **WCAG AA**.

## §5 Per-surface rules
- **Take-quiz:** flat, calm, low-anxiety. No gradients. Calm ever-present `ProgressBar`. No countdown-timer language.
- **Teacher dashboard:** the **one** place a blueberry gradient hero is allowed.
- **Results:** encouraging headline + calm score ring + counts framed as "to review" (`ResultSummary`).
- **AI feedback:** the coral/apricot `AIFeedbackCard` — visually distinct and supportive, a helpful voice, **not a grade stamp**.

## §6 Cards
White surface, **20px** radius, hairline border **plus** a soft warm shadow, **24px** padding. The most-used surface (classrooms, quizzes, results, settings). **Never** a coloured-left-border-only card (explicit anti-pattern).

## §7 Interaction states (required on every interactive surface)
- **Buttons:** hover darkens the fill; press shrinks (`--press-scale` .97). Ghost/secondary: soft sand or tinted wash on hover.
- **Interactive cards:** hover lifts (`--lift-y` -2px) + deeper shadow; press barely shrinks.
- **Answer tiles:** hover warms the border + faint sand fill; selected = blueberry 2px border + tint; on review, correct/incorrect = green/rose 2px border.
- **Focus:** always-visible high-contrast blueberry glow (`--focus-ring-shadow`); fully keyboard-navigable.
- Provide **default / hover / press / disabled / focus**, plus **loading / empty / error** where the surface can be in those states.
- **All motion respects `prefers-reduced-motion`.**

## §8 Icons
Phosphor via the `Icon` wrapper — `regular` idle, `fill` active/selected, `bold` inside buttons. No hand-drawn SVG icons; no emoji/unicode as icons.

## §9 Accessibility (non-negotiable)
AA contrast on every pairing, full keyboard navigation, always-visible focus, 44px minimum targets, screen-reader labels on meaningful icons/controls, and reduced-motion support. This is a `foundation.md` §2 requirement, not a nice-to-have.
