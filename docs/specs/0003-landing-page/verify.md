# 0003. Quiztin marketing landing page — verify

How to prove each acceptance criterion once the page is built. Run the app locally (`docker compose up` for the backend, then `cd frontend && npm run dev`) unless a step says to check the production build. Acceptance criteria live in [index.md](index.md).

## Rendering and routing
- **AC-1**: Load `/` logged out. The landing page renders, with no redirect to sign in. Load `/` with a valid session. The same landing page renders, with no forced redirect.
- **AC-5**: Logged out, the nav shows "Sign in" and "Get started". With a session, the nav shows a link into the app (`/profile` today) instead of "Get started".
- **AC-4**: Click "Get started". You reach `/register`. Click "Sign in". You reach `/signin`. The "For teachers" call to action lands on register with the instructor role preselected or hinted.

## Hero toggle
- **AC-2**: The hero shows a "For students" and "For teachers" segmented control, defaulting to students. Switching updates the headline, subcopy, call to action, and visual. Operate it by keyboard (tab to it, arrow or enter to switch). The active option is exposed to assistive tech (a pressed state, or a radio group with a checked option), and the content change is announced too (an `aria-live` region updates, or focus moves into the changed hero). Confirm there is no automatic rotation.

## Sections
- **AC-3**: The sections appear in this order, top to bottom: public nav, hero, how it works (three steps), value split (for teachers and for students), AI feedback spotlight, FAQ, free for classrooms line, closing call to action band, footer. None missing, none out of order.

## Brand and tokens
- **AC-6**: Inspect computed styles. Colours, type, radius, and shadow all resolve from `--` token aliases, with no raw hex in the landing styles. Fredoka on headings, Nunito on body. Count blueberry gradients on the page: at most one, in the hero. Confirm `ui-rules.md` §5 now names the landing hero as an allowed gradient surface.
- **AC-7**: Every section shows the decoration motif. Run a contrast checker on text over decorated areas. All pairings meet WCAG AA.
- **AC-8**: Persona photos sit in blob cutout frames with a coral or sand duotone. unDraw illustrations appear only in the process and decorative zones. No single block mixes a photo person and an illustrated person.

## Honesty
- **AC-9**: Product visuals are vignettes built from tokens and primitives, not screenshots of unbuilt screens. There is no invented testimonial or attributed quote anywhere on the page.
- **AC-15**: A visible line of microcopy discloses that the persona photographs are illustrative and generated (for example near the images or in the footer). A visitor cannot mistake them for real named users.

## Motion
- **AC-10**: With motion allowed, the ambient drift, scroll reveal, toggle crossfade, and highlighter draw on all play. Set the OS or dev tools to `prefers-reduced-motion: reduce` and reload. None of that motion runs, and every section is fully visible and usable. The reduced motion path (content fully visible, no hidden or shifted start state) is also asserted in `src/features/landing/motion/motion.test.tsx`.

## SEO, prerender, and serving
- **AC-11**: Build the frontend (`npm run build`). The prerendered file is `dist/index.prerender.html`; it contains the hero copy and section text with no JavaScript (open the file, or curl the served route). Confirm the `<title>`, meta description, Open Graph and Twitter tags, canonical link, and product JSON-LD are present in it. The prerender output is also asserted in `src/prerender.test.tsx` and the head builder in `src/features/landing/seo.test.ts`.
- **AC-14**: The build emits two documents: `dist/index.prerender.html` (the landing, served only at exact `/`) and `dist/index.html` (the neutral bootstrap, the SPA catch all). The gateway (`src/Gateway/Program.cs`) serves the prerender through an explicit `MapGet("/")` endpoint and keeps `index.html` as `MapFallbackToFile`. Run the gateway over the built `wwwroot` and curl: `GET /` returns the landing markup plus the SEO tags; `GET /register` and `GET /profile` return the neutral bootstrap (generic title, no landing markup or Open Graph meta). No client route other than `/` shows landing content to a no JavaScript client. `src/prerender.test.tsx` asserts the neutral bootstrap stays landing free.

## Responsive and accessibility
- **AC-12**: At a 360px width, the hero toggle and every section reflow cleanly, with no horizontal scroll. Tap targets are at least 44px. Tab through the whole page. Focus is visible on every control. Run axe (the project already uses vitest-axe). No violations.

## Performance
- **AC-13**: Inspect the build output. The landing route and framer-motion are in their own chunk (`dist/assets/LandingPage-*.js`, plus `LandingPage-*.css`), not in the authenticated app's main entry chunk (`dist/assets/index-*.js`). Load a non `/` route (for example `/register`) and confirm, via the Network panel or `performance.getEntriesByType('resource')`, that only the main entry chunk loads: no `LandingPage` chunk, no framer-motion, no persona images.

## Regression
- Build green (`npm run build`, which also runs the prerender, `npm run lint`, `tsc --noEmit`), and the tests pass (`npm run test`, 57 including 27 for the landing). The only backend touch is the gateway serving endpoint (`src/Gateway/Program.cs`); the services are untouched, and `dotnet build` plus `dotnet test` stay green.
