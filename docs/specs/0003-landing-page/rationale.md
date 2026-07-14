# 0003. Quiztin marketing landing page — rationale

The decision record for [index.md](index.md): the why, the options, and what this rests on. `/develop` builds from `index.md` and can skip this file.

## Context

> ⚠️ Premise note: two things shape this design and are worth stating plainly. First, the core loop screens (quiz taking, results, AI feedback) are not built yet, so there are no real screenshots of them to show; a marketing page that implies otherwise would be dishonest. This spec resolves it by composing on brand interface vignettes from the real design tokens, and flags the swap to real captures as follow up. Second, this page adds one frontend dependency (framer-motion), which runs against the project's few and explicit dependencies ethos (foundation §7 #21); the developer chose it consciously, for richer motion. The prerender for SEO adds no package: as built it reuses `react-dom/server` `renderToString`, already present through react-dom.

Quiztin is deployed and functional (spec 0002), but it has no public front door. A first time visitor reaches the sign in screen with no explanation of what Quiztin is, who it is for, or why it is trustworthy.

The product serves two audiences doing opposite jobs: teachers author and publish quizzes inside classrooms, and students join and take them. A single hero that tries to address both at once shortchanges one. The page has to speak to each without burying the other.

The brand and design system are locked, generated from the Claude Design export (`design-system/`, plus the ui trio and the `quiztin-design` skill). The look is warm, calm, rounded, and light only, with a strict token contract (semantic aliases, no raw hex) and firm rules (coral is sparing and is the AI voice, one blueberry gradient moment is allowed, all motion yields to reduced motion). A marketing page may be a little louder than the in app calm, but it must not fork the system.

The app is a client rendered single page app served single origin by the YARP gateway, with server side rendering deliberately rejected (foundation §7 #5 and #25). That is right for the app, but it fights discoverability for a public marketing page, whose whole job is to be found and shared.

Not deciding leaves the product with no way to explain or sell itself, and every visitor arriving cold at a login form.

## Options considered

### Option 1: Pure client rendered page, on brand, CSS only motion, no personas

One more React route inside the SPA, styled from the tokens, animated with CSS, no new dependencies and no generated imagery.

**Pros**:
- Zero new dependencies; nothing to add to the approved list.
- Fastest to ship, least build complexity.

**Cons**:
- Poor search and social presence, because a client rendered page serves crawlers an empty shell.
- Less warmth and less proof without personas or richer motion, which is exactly what the developer asked for.

### Option 2 (chosen): On brand landing page at `/`, decoration kit, personas and vignettes, framer-motion, build time prerender

One public page at the root, composed from the design system, carrying the decoration kit on every section, real persona faces and friendly illustrations, on brand interface vignettes for product proof, framer-motion for motion, and a build time prerender for SEO.

**Pros**:
- Real search and social presence through static prerendering, without adopting runtime server rendering.
- Rich, warm, and on brand, serving both audiences through the hero toggle.
- Produces reusable assets (the decoration kit, personas, vignettes) for later marketing.

**Cons**:
- Two new frontend dependencies and a more complex build.
- The vignettes are not real screenshots and must be maintained until the real screens exist.

### Option 3: A separate marketing site outside the app

Build the landing page in a dedicated static site generator or a hosted page builder, deployed apart from the app.

**Pros**:
- Best in class marketing and SEO tooling.
- Fully decoupled from the app's release cycle.

**Cons**:
- A second toolchain and a second deployment to run and pay for.
- Forks the design system into a second place and risks brand drift.
- Overkill for a single page, and it fights the single origin gateway model the platform is built on.

### Option 4: Extend the brand through Claude Design for a bespoke marketing look

Use Claude Design to evolve a distinct marketing visual language for the page.

**Pros**:
- Potentially more distinctive than reusing the app's system.

**Cons**:
- Forks the locked design system for no strong reason, since the brand is already settled.
- More time and cost, and a second visual language to maintain.

## Rationale

Option 2 wins because it is the only one that satisfies the two forces in tension: a public page has to be found and shared (which the SPA's no server rendering stance blocks), and the brand must not fork (which a separate site or a Claude Design reskin would do). A build time prerender resolves the first without reopening the second: it is static generation of one public route at build, not runtime server rendering, so it respects foundation §7 #5 and #25 while still handing crawlers real HTML (basis: foundation §7 #5 and #25; static site generation practice).

The prerender is done with a small postbuild script calling `react-dom/server`, which is already available through react-dom, so it adds no new package; the dedicated tool `vite-react-ssg` was not needed. As built the script uses `renderToString` (not `renderToStaticMarkup`), so the prerendered `/` hydrates in place rather than repainting: the script renders the same app tree the client hydrates, differing only in the router (a memory router pinned to `/` for the prerender, the browser router in the app), which keeps `useId` and hydration aligned. This settled the router question the design flagged: the app pins react-router `^8.2.0` (a drift from the v7 the context named, now reconciled), and 8.2.x supplies `createMemoryRouter`, `RouterProvider`, and `renderToString` as verified. The static markup is served only at exact `/`, through an explicit gateway endpoint that outranks the SPA fallback, with the neutral bootstrap `index.html` kept as the catch all, so the gateway does not hand the landing page's HTML to `/register` or any other deep link.

The hero toggle is chosen over an auto advancing carousel deliberately. Rotating hero carousels are a known weak pattern: viewers miss the second slide, the moving call to action hurts conversion, and the motion works against a brand whose whole promise is calm and accessible. A user driven segmented control gives the same two vibes, lets the visitor choose, doubles as audience routing, and adds no motion, which fits the brand and the accessibility bar (basis: `ui-rules.md` §0 and §7).

The motion approach is framer-motion. The cross check surfaced that the app has no animation library today and that CSS keyframes plus a small `IntersectionObserver` would cover the `ui-rules.md` §7 motion vocabulary with zero new dependency, so the developer weighed that and reaffirmed framer-motion for its richer, spring based choreography and its first class reduced motion support (`useReducedMotion`). The tradeoff is one new runtime dependency, recorded in `library-docs.md` (basis: foundation §7 #21; `ui-rules.md` §7).

## Cross check

An independent model read the drafted spec against the actual codebase and returned a needs rework verdict. Its findings were folded in before acceptance: the gateway fallback would have served the landing markup for every route (now AC-14, with the serving fix in Feature design); `/` was already claimed by the authenticated index redirect, so mounting the landing there is a reroute (now named in build task 1); the gradient rule cited `ui-rules.md` §5 without updating it (now a required reconciliation, AC-6 and Follow-up); the prerender tool was unverified against the real router, and a zero dependency `react-dom/server` path exists (the chosen approach, built with `renderToString` for clean hydration); and three smaller gaps (no verify step for section order, the toggle announcing only its own state, and undisclosed personas) became AC and verify additions (AC-3 check, AC-2 content announcement, AC-15).

The interface vignettes over screenshots choice is forced by honesty: the quiz taking, results, and AI feedback screens do not exist yet (only the primitives and the auth and profile screens are built, per `ui-registry.md`), so a screenshot of them would be fabricated, and an invented testimonial would be worse. Composing vignettes from the real tokens keeps the page truthful and still on brand, and the swap to real captures is queued for when those screens ship.

The personas are generated rather than stock for two reasons: continuity (the same three faces can recur across the hero, the how it works steps, and the spotlight, which stock cannot provide) and rights (free stock grants no model release, so using a real face as a representative Quiztin user edges into implied endorsement; a generated face does not).

## References

**Project sources** (verifiable, in this repo):
- `context/ui-rules.md` (§0 prime directive, §4 colour discipline, §5 per surface rules, §7 motion and reduced motion) and `context/ui-tokens.md` (the semantic token contract).
- `context/foundation.md` §7 #5 and #25 (no server rendering, no meta framework), #21 (few and explicit dependencies), #24 (styling bound to tokens).
- `context/ui-registry.md` (which components are built: the primitives, plus auth and profile; the quiz, results, and AI feedback components are still planned).
- Spec 0001 (the frontend foundation and the auth screens the calls to action reuse) and spec 0002 (the gateway and single origin serving model).
- The `quiztin-design` skill (`.claude/skills/quiztin-design/`), the branded UI and asset authority.

**Practices & standards**:
- Static site generation (prerendering one public route at build) over runtime server rendering, to gain SEO without adopting a server rendering framework.
- A segmented audience control over an auto advancing hero carousel, for accessibility and conversion.
- Representative personas, never fabricated testimonials, for honest marketing.
- `prefers-reduced-motion` respected for all motion (WCAG).
