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
- **AC-10**: With motion allowed, the ambient drift, scroll reveal, toggle crossfade, and highlighter draw on all play. Set the OS or dev tools to `prefers-reduced-motion: reduce` and reload. None of that motion runs, and every section is fully visible and usable.

## SEO, prerender, and serving
- **AC-11**: Build the frontend (`npm run build`). The emitted HTML for `/` contains the hero copy and section text with JavaScript disabled (open the built file, or curl the prerendered route). Confirm the `<title>`, meta description, Open Graph and Twitter tags, canonical link, and product JSON-LD are present in that HTML.
- **AC-14**: In the build output, the prerendered landing markup lives at a target the gateway serves only for exact `/`, and the SPA fallback (`index.html`) is a neutral bootstrap, not the landing page. Curl the deployed `/register` (or open the built fallback) with JavaScript off: it returns the neutral bootstrap, not the landing HTML, title, or meta. Curl `/`: it returns the landing markup. No client route other than `/` shows landing content to a no JavaScript client.

## Responsive and accessibility
- **AC-12**: At a 360px width, the hero toggle and every section reflow cleanly, with no horizontal scroll. Tap targets are at least 44px. Tab through the whole page. Focus is visible on every control. Run axe (the project already uses vitest-axe). No violations.

## Performance
- **AC-13**: Inspect the build output. The landing route and any motion library are in a separate chunk, not in the authenticated app's main entry chunk. Loading the app after sign in does not download the marketing only assets.

## Regression
- Build green (`npm run build`, `npm run lint`, `tsc --noEmit`), and the new component tests pass (`npm run test`). The backend is untouched.
