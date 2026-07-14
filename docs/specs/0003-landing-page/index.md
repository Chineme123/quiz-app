# 0003. Quiztin marketing landing page

**Date**: 2026-07-13
**Status**: In Progress

## Summary

Quiztin is deployed and works, but it has no public front door: a visitor lands straight on the sign in screen with no idea what Quiztin is or who it serves. This spec designs a public marketing landing page at `/`, built entirely from the locked design system (turned up a little, not a new look). It speaks to both audiences through one hero with a "For students" and "For teachers" toggle, carries a light bubble and blob motif on every section, and uses real personas, friendly illustrations, and on brand interface vignettes for warmth and proof. It ships prerendered to static HTML so search engines and social cards work, with motion that always yields to reduced motion.

## Requirements

**User stories**:
- As a visiting teacher, I want to see what Quiztin does and that it is credible and free, so that I decide to create my first quiz.
- As a visiting student, I want the page to feel welcoming and low pressure, so that joining a classroom feels easy, not like a test.
- As a returning user, I want the front door to recognise me, so that I can step back into the app quickly.
- As anyone sharing the link, I want a proper title, description, and share card, so that the link looks trustworthy.

**Acceptance criteria** (the contract, each independently checkable):
- **AC-1**: A public landing page renders at `/` for every visitor, signed in or not, with no authentication required and no forced redirect.
- **AC-2**: The hero carries a "For students" and "For teachers" toggle (a segmented control, never an auto advancing carousel). Switching updates the headline, subcopy, primary call to action, and hero visual for that audience. The default tab is "For students". The control is fully keyboard operable, its own active state is announced to assistive tech, and the resulting hero content change is announced too (through an `aria-live` region or by moving focus), so a screen reader user knows the rest of the hero changed.
- **AC-3**: The page presents these sections in order: public nav, hero, how it works (three steps), value split (for teachers and for students), AI feedback spotlight, FAQ, a free for classrooms line, a closing call to action band, and a footer.
- **AC-4**: Primary calls to action route to the existing auth screens: "Get started" goes to `/register`, "Sign in" goes to `/signin`. The "For teachers" call to action hints the instructor role on the register screen.
- **AC-5**: The public nav adapts to auth state: a signed out visitor sees "Sign in" and "Get started"; a signed in visitor sees a link into their app (today `/profile`, the built entry, later a dashboard) instead of "Get started".
- **AC-6**: Every colour, type, radius, and shadow comes from the semantic design tokens (no raw hex, no off palette value). Headings use Fredoka, body uses Nunito. The one blueberry gradient moment appears at most once, in the hero, and this second allowed gradient surface is recorded in `ui-rules.md` §5 (updated from teacher dashboard only), so the spec does not license it against a context file that forbids it.
- **AC-7**: Every section carries the decoration motif (bubbles, blobs, squiggles, dots, wave dividers, highlighter marks) as on brand inline SVG. The busy background never drops any text pairing below WCAG AA contrast.
- **AC-8**: Persona photographs appear inside organic blob cutout frames with a coral or sand duotone treatment. Illustrated people (unDraw, recoloured to the palette) appear only in the process and decorative zones. The two people styles never mix inside the same block.
- **AC-9**: Product visuals are on brand interface vignettes composed from the real design tokens and primitives, because the quiz taking, results, and AI feedback screens are not built yet. The page shows no screenshot of a screen that does not exist, and no invented testimonial or attributed quote.
- **AC-10**: Motion is powered by framer-motion with a genuine reduced motion path (framer-motion's `useReducedMotion`). When `prefers-reduced-motion: reduce` is set, ambient motion and scroll reveals do not run, and all content is fully visible and usable without them.
- **AC-11**: The landing route is prerendered to static HTML at build time, so a crawler that runs no JavaScript still receives the hero copy and section text. The page ships a title, a meta description, Open Graph and Twitter card tags, a canonical URL, and product JSON-LD.
- **AC-12**: The page is responsive from a 360px width up: the hero toggle and every section reflow cleanly, tap targets are at least 44px, and the whole page is keyboard navigable with an always visible focus ring.
- **AC-13**: The landing route and its marketing only dependencies and large decorative or persona assets are code split, so they do not enlarge the authenticated app's initial download.
- **AC-14**: The prerendered landing markup is served only for an exact `/` request. Every other client route (`/signin`, `/register`, `/profile`, any deep link) still receives a neutral SPA bootstrap document, never the landing page's HTML, title, or meta. The gateway catch all fallback keeps serving that neutral document, not the landing markup.
- **AC-15**: The page discloses, in visible microcopy, that the persona photographs are illustrative and generated, so no visitor reads them as real named users.

## Decision

**Chosen option**: Option 2: an on brand landing page at `/`, composed from the design system, with a decoration kit, personas and vignettes, and a build time prerender.

Build one public marketing page at the app root, entirely inside the locked Quiztin design system (dialed up a little, not reskinned), served by the same SPA and gateway as the rest of the app, and prerendered to static HTML for search and social.

The prerender uses a small postbuild script calling `react-dom/server` `renderToStaticMarkup` for the single `/` route. react-dom is already a dependency, so this adds no new prerender package; a dedicated tool (`vite-react-ssg`) is only a fallback if the script cannot meet the criteria, and either path is prototyped against the real entry first (see Feature design). Motion uses framer-motion, which the developer reaffirmed after the cross check surfaced a CSS only alternative; it brings first class reduced motion support (`useReducedMotion`).

**Implementation skills**: `quiztin-design` (the Quiztin branded UI and asset skill, `.claude/skills/quiztin-design/`) governs the look, colour, type, and token use for every part of this page.

## Feature design

**Data model sketch**:
No new data model. The landing page holds no server state and persists nothing. All content (headlines, the three steps, value points, FAQ entries, footer links) is static in the frontend. The page reads the existing auth context only to choose the nav variant, and it reuses the existing auth screens for its calls to action.

**State transitions**:
Two small pieces of client only UI state, no domain state machine:
- Hero audience: `students` (default) or `teachers`, switched by the toggle.
- Nav variant: `signed out` or `signed in`, derived from the existing auth context, not new state.

**Routing and serving** (load bearing, corrected after the cross check):
- `/` today resolves to the authenticated shell: `router.tsx` mounts `RequireAuth` and `AppShell` as pathless layout routes whose index child does `Navigate to="/profile"`, so bare `/` already redirects an anonymous visitor to sign in. This build removes that index redirect and renders the public `LandingPage` at `/` outside `RequireAuth`, for everyone. The signed in nav links into the app at `/profile` (the only built authed screen today; it becomes the dashboard when one exists).
- Serving: the SPA is baked into the gateway `wwwroot` and served with a catch all fallback to a bootstrap `index.html` (`src/Gateway/Program.cs`, `MapFallbackToFile`). The prerendered landing markup must not overwrite that bootstrap `index.html`, or every non file route would fall through to the landing content and a no JavaScript crawler at `/register` would get the wrong page. Emit the static `/` HTML to a target the gateway serves only for an exact `/` match, and keep the neutral bootstrap `index.html` as the generic SPA fallback. The landing entry hydrates its prerendered markup rather than repainting from empty, so a JavaScript visitor sees no flash of blank or wrong content.

**API surface**:
No new endpoints. The landing page issues no API calls of its own. Its calls to action navigate to `/register` and `/signin`, which use the existing `/api/auth` endpoints through screens that already exist (spec 0001).

| Surface | Method | Key inputs | Key outputs | Auth | Key errors |
|---|---|---|---|---|---|
| (none new) | | | | public | |

**Key invariants**:
- Tokens only: every value is a semantic alias, never a raw hex or off palette colour (the design system contract in `ui-tokens.md`).
- At most one blueberry gradient on the page, in the hero, and `ui-rules.md` §5 is updated to name the landing hero as an allowed gradient surface before build.
- Coral stays the sparing signature and the AI voice; it is not spread everywhere just because this is marketing (`ui-rules.md` §4).
- Decoration is always a background layer; it never lowers a text pairing below WCAG AA.
- Honesty: no screenshot of a screen that is not built, no invented testimonial, and the personas are visibly disclosed as illustrative.
- All motion yields to `prefers-reduced-motion`.
- The prerendered landing markup is served only at exact `/`; the SPA fallback stays neutral.

**Security model**:
A public, unauthenticated, read only marketing surface. It exposes no user data and adds no new authorization. It reads the existing session state only to swap the nav label, issuing no new tokens. Signed in visitors are allowed to view it (the chosen routing). The prerendered HTML is fully public content and must contain no secret or private data.

**Configuration required**:
No new runtime environment variables. The build gains framer-motion as a runtime dependency. The prerender uses `react-dom/server` `renderToStaticMarkup`, already present through react-dom, so it adds no new prerender package. A canonical base URL and an Open Graph image are build config, not secrets.

**Critical test scenarios**:
- Happy path: a visitor loads `/`, toggles to "For teachers", clicks "Get started", and reaches `/register` with the instructor role hinted. Verifies AC-1, AC-2, AC-4.
- Serving: the built `/register` route, fetched with JavaScript disabled, returns the neutral bootstrap document, not the landing page's HTML; a fetch of `/` returns the prerendered landing markup. Verifies AC-14.
- Reduced motion: with `prefers-reduced-motion: reduce`, no ambient or scroll motion runs and every section is fully visible and usable. Verifies AC-10.
- Prerender and SEO: the built HTML for `/` contains the hero copy, the meta description, and the Open Graph tags with JavaScript disabled. Verifies AC-11.
- Accessibility: a keyboard only visitor operates the nav, the hero toggle, and the calls to action with a visible focus ring; the toggle announces both its state and the content change; and an automated axe pass finds no violations. Verifies AC-2, AC-12.
- Auth adaptive nav: a signed in visitor sees a link into the app instead of "Get started". Verifies AC-5.
- Contrast: text over decorated backgrounds holds WCAG AA everywhere. Verifies AC-7.

## Build plan

No build approach is recorded in the context system for this feature, so this plan uses a thinnest usable page first ordering (a Skateboard and Facade blend), fitting for a static marketing surface: stand up a real, viewable page at `/` first, then thicken it section by section, then layer on the rich assets, motion, and SEO.

1. Public shell and routing: add a `PublicLayout` (marketing nav and footer); remove the authenticated index redirect that currently sends bare `/` to `/profile` in `router.tsx`, and mount `LandingPage` at `/` outside `RequireAuth` for everyone; make the nav adapt to auth state (signed in links to `/profile`); wire the calls to action to `/register` and `/signin`. Satisfies AC-1, AC-4, AC-5.
2. Landing style layer and decoration kit: bind any dialed up accent values through the existing token theme, and build the in repo SVG decoration components (bubble, blob, squiggle, dot, wave divider, highlighter), all tokenised. Satisfies AC-6, AC-7.
3. Hero with the audience toggle: a keyboard operable segmented control (default "For students") that announces both its own state and the content change, audience specific copy, call to action, and visual, plus the single allowed gradient moment. Satisfies AC-2, AC-6.
4. Content sections in order: how it works (three steps joined by squiggle connectors), the value split (teacher cards and student cards), the AI feedback spotlight (coral), the FAQ, the free for classrooms line, the closing call to action band, and the footer. Satisfies AC-3.
5. Human and illustration assets: add the persona photographs as static assets and build the blob cutout plus duotone treatment, with the visible microcopy disclosing they are illustrative; add unDraw illustrations recoloured to the palette into the process zones; keep the two people styles in separate blocks. Satisfies AC-8, AC-15.
6. Product vignettes: compose on brand interface vignettes from the design tokens and primitives to stand in for the quiz taking, results, and AI feedback screens (not built yet); use real screenshots only for screens that exist. Satisfies AC-9.
7. Motion: with framer-motion, add ambient bubble drift, scroll reveal, toggle crossfade, and the highlighter draw on, every one gated behind a reduced motion check (framer-motion `useReducedMotion`). Satisfies AC-10.
8. SEO, prerender, and serving: prerender the `/` route to static HTML with a small postbuild script using `react-dom/server` `renderToStaticMarkup` (no new dependency; `vite-react-ssg` only as a fallback, prototyped against the real `createBrowserRouter` and `createRoot` entry first); emit it to a target the gateway serves for an exact `/` only while the neutral bootstrap `index.html` stays the catch all fallback; hydrate rather than repaint; and ship the title, meta description, Open Graph and Twitter tags, canonical URL, and product JSON-LD. Satisfies AC-11, AC-14.
9. Responsive and accessibility pass: reflow from 360px, 44px targets, visible focus, and a clean axe run. Satisfies AC-12.
10. Performance: code split the landing route and its marketing only dependencies so the authenticated app's first load stays lean. Satisfies AC-13.
11. Tests: cover the toggle (state and content announcement), the auth adaptive nav, and call to action routing; a reduced motion test; an axe accessibility test; a serving test (a non `/` route does not get landing markup); and a prerender assertion on the built HTML. Satisfies AC-2, AC-5, AC-10, AC-11, AC-14, AC-12.

## Consequences

**Positive**:
- A real front door that speaks to both audiences at once, without picking one.
- Unmistakably on brand, because it is built from the same tokens and skill as the app.
- Genuine search and social presence for a client rendered SPA, through static prerendering.
- A reusable decoration kit and a persona and vignette asset set for future marketing surfaces.
- The prerender needs no new package (it reuses `react-dom/server`), so the only new runtime dependency is framer-motion.

**Negative / tradeoffs**:
- The prerender and the gateway fallback interact in a way that will ship a bug if built naively: the static `/` markup and the neutral SPA bootstrap must be different targets, and the gateway must serve the static one only at exact `/`. This is now a named criterion (AC-14), not a hope.
- Mounting the landing at `/` is a reroute: the authenticated index redirect must move off `/`, touching `router.tsx`.
- The interface vignettes are not real screenshots, so they carry drift risk and must be swapped for real captures when the core loop screens ship.
- Signed in visitors see the marketing page at `/` (a small friction the developer accepted, in exchange for a simpler routing rule).
- More imagery means more page weight, offset by code splitting and lazy loading, not eliminated.
- The context says react-router v7 but `frontend/package.json` pins `^8.2.0`; the prerender entry wiring depends on the real version, so this drift must be reconciled first.

**Neutral**:
- Introduces a public layout separate from the authenticated app shell.
- `ui-rules.md` §5 gains the landing hero as a second allowed gradient surface (a context edit this spec requires).
- unDraw is attribution free and the personas are ours to use, so no licensing debt, but the personas are representative, disclosed as generated, and never captioned as real users.
- The landing page becomes the first surface that must be kept in visual sync with the design system as it grows.

## Follow-up

- [ ] Add framer-motion to `library-docs.md` approved dependencies with a why and how used (the list gates additions, foundation §7 #21). The prerender adds no new package (it reuses `react-dom/server` `renderToStaticMarkup`); record that approach in `library-docs.md`, and only add `vite-react-ssg` if the script path is abandoned.
- [ ] Reconcile the React Router version drift: `frontend/package.json` pins react-router `^8.2.0`, but foundation §7 #25 and `library-docs.md` say v7. Update the context to match the installed major (or pin back) before the prerender entry is wired, since the entry contract differs by version.
- [ ] Update `ui-rules.md` §5 to name the marketing landing hero as an allowed blueberry gradient surface alongside the teacher dashboard (a reconciliation AC-6 requires; do it before build, add a `progress-log.md` note).
- [ ] Swap the interface vignettes for real screenshots once the quiz taking, results, and AI feedback screens are built.
- [ ] Choose and add the Open Graph share image and a canonical base URL as build config.
- [ ] Consider self hosting the Fredoka and Nunito woff2 for the public page (the export flags CDN fonts; a public page benefits from self hosted fonts for speed and privacy).
- [ ] If the personas must recur with identical faces across the tutoring and office scenes, consider training reusable persona characters, since plain generation does not guarantee an exact match.

## Rationale

Reasoning, options, context, and references: see [rationale.md](rationale.md). Verification steps: see [verify.md](verify.md).
