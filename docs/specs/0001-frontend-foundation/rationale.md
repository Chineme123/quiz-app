# 0001. Rationale

The decision record for [index.md](index.md). Builds do not read this file.

## Context

Quiztin's backend reached the end of Layer 0. The framework is pinned, Postgres is live, scoring grades against real answers, and AuthService issues tokens carrying a canonical `Guid`. A design system arrived as an export and produced `context/ui-tokens.md`, `ui-rules.md`, and `ui-registry.md`. What does not exist is any frontend at all, and the decision of what that frontend is made of has to be taken before a single component is written, because the choices below are the kind that are cheap now and very expensive in three months.

Three facts found during exploration constrain every option, and each of them was surprising enough to be worth stating plainly.

The design system ships no React source. It contains eight files of CSS custom properties, a handoff document with per component prop specifications, and `_ds_bundle.js`, which is a Babel compiled browser global that assigns to `window.QuiztinDesignSystem_138691` and has no imports or exports. Vite cannot meaningfully import it. Whatever plan gets chosen, all 18 components in the registry are written by hand, and the export accelerates only the styling decisions, not the code.

A browser cannot call this backend at all today. There is no CORS configuration in any of the five services, there is no gateway yet, `UseHttpsRedirection()` is enabled everywhere, and the development ports (5005, 5079, 5224) disagree with the docker ports (5001, 5002, 5224). Any plan that has the app calling service origins directly is not a plan, it is a bug report.

Most screens have no data source. Nothing lists a teacher's classrooms, nothing lists a classroom's quizzes, and no results service exists beyond an empty stub. Auth and Manage Profile are the only two surfaces with real endpoints behind them. Building anything else means building against a mock, and a mock is a second source of truth that will quietly diverge from the first.

Beneath all of it sits the session question. AuthService issues an 8 hour JWT and hands it to the caller. If the app stores that token where JavaScript can read it, then a single injected script anywhere on the page walks away with 8 hours of authenticated access to a student's academic record. `security.md` is explicit that student data is the thing being protected. Not deciding this now means deciding it by default, and the default is the insecure one.

## Options considered

### Option 1: The conventional SPA

React and Vite, CSS Modules for styling with the token variables consumed directly, plain `fetch` calls, and the 8 hour JWT kept in `localStorage`. Screens built against the two real endpoints.

**Pros**

- The least new machinery. Nothing to learn, nothing to configure.
- CSS Modules have no build time coupling to the token files, so the design system can change independently.
- No backend change is needed at all. Ship the frontend and stop.

**Cons**

- The token in `localStorage` is readable by any script that runs on the page. For an 8 hour token guarding student records, this is the wrong answer and no amount of care elsewhere compensates for it.
- Nothing prevents a raw hex value being typed into a stylesheet. The tokens only rule survives on review discipline alone, which is a rule that decays.
- Every component reimplements its own state and cache handling, or does without.

### Option 2: Token bound Tailwind behind a same origin proxy, with a rotating refresh cookie

React, Vite, and TypeScript. Tailwind v4 whose theme is bound by reference to the existing custom properties through `@theme inline`, with Preflight left out because the design system's `base.css` is already the reset. TanStack Query for server state. A short lived access token held in a JavaScript variable, and a rotating refresh token in an `HttpOnly` cookie, which requires two new AuthService endpoints. The Vite dev proxy makes the app same origin with the services, and the planned YARP gateway does the same later.

**Pros**

- No durable credential is reachable by a script. This is the whole point.
- `@theme inline` compiles `bg-primary` to `var(--primary)` rather than to a copied colour value, so the tokens only rule is enforced by the compiler.
- The proxy sidesteps CORS entirely rather than adding CORS in five places, and it is the same shape the gateway will take.
- TanStack Query supplies request deduplication, caching, and retry policy, which are the exact problems this app has.

**Cons**

- Two new backend endpoints, a new table, and a migration, before the frontend can even boot.
- Rotating refresh tokens introduce a genuinely subtle failure mode where two tabs racing on page load look identical to a stolen token. It must be designed for on both sides.
- Disabling Preflight silently breaks Tailwind's `border` utilities until a rule restores `border-style: solid`.
- Tailwind's utility classes are a build time dependency on the token names.

### Option 3: React Router framework mode with a server held session

Adopt React Router v7's framework mode, with its Vite plugin, file based routes, server rendering, loaders, and actions. The session lives entirely on a Node server that sits in front of the .NET services, and the browser holds nothing.

**Pros**

- The most secure session model available. The browser never sees a token of any kind.
- Loaders remove most client fetching code, and server rendering gives a fast first paint.

**Cons**

- It adds a Node server to a .NET microservice architecture, which means a second runtime to deploy, monitor, and keep alive on Railway. It becomes a third tier in front of a gateway that does not exist yet.
- This is the same machinery the project already rejected once, in foundation §7 #5, when it rejected Next.js on the grounds that there is no server rendering or search engine requirement. Nothing about that reasoning has changed.
- The gain over Option 2 is real but small, and it is bought with a permanent operational cost.

### Option 4: Facade first, importing the bundle as is

Load `_ds_bundle.js` with a script tag, build every screen quickly against mocked endpoints, and wire the real backend later.

**Pros**

- The fastest route to something that looks finished. Every screen in the registry could exist within days.
- No backend work at all up front.

**Cons**

- The bundle expects a global React and exports through a window property. Wiring Vite around it means fighting the module system on every component.
- A mock layer is a second source of truth. It agrees with the backend exactly until it does not, and the divergence is discovered at integration.
- The build approach in foundation §0 is breadth first, not prototype first, and the developer stated the constraint directly: build on what exists.

## Rationale

Option 2 was chosen because the session question dominates everything else. `security.md` exists because student academic records are the asset, and Option 1 hands an 8 hour credential to any script that manages to run on the page. That is not a tradeoff, it is a defect, and it is the one decision here that cannot be walked back cheaply once users exist. Option 3 solves the same problem more completely, and was rejected because the cost is a permanent second runtime in a .NET architecture, bought to close a gap that a refresh cookie already closes. The project rejected exactly this machinery once already, for reasons that still hold.

The Tailwind binding was chosen for a reason that has nothing to do with Tailwind. The rule that matters is the one in `ui-rules.md`: semantic tokens only, no raw hex. A rule enforced by review is a rule that erodes the first time someone is in a hurry. `@theme inline` makes the utility resolve to the variable, so a developer reaching for a raw colour has to leave the system to do it. That turns a convention into a constraint, which is the only kind that survives. It costs a build time coupling to the token names and one sharp edge around Preflight, both of which are cheap and both of which are documented in the build plan.

The proxy follows directly from the second exploration finding. There is no CORS anywhere, and adding it to five services would be five places to get it wrong, then a sixth when the gateway lands. Making the app same origin instead means CORS is never a concept the app has to know about, in development or in production, and it means the move to the gateway changes a configuration file rather than a client.

Restricting scope to auth and Manage Profile is the developer's own instruction, and exploration confirmed it was the right one. There is no endpoint that lists a classroom, a quiz, or a result. Option 4's mocks would have papered over that, and the papering is worse than the gap, because a mock that drifts from the backend is discovered late and trusted until then. Manage Profile is also the only screen in scope with an actual design brief, `docs/uc14-ui-ux-brief.md`, which means it can be built to a specification rather than to taste.

## Supporting evidence

**Exploration findings, verified against the code**

- `design-system/` contains `styles.css`, `tokens/*.css`, `readme.md`, `HANDOFF.md`, `SKILL.md`, and `_ds_bundle.js`. No React source, no `package.json`.
- No `AddCors` or `UseCors` call exists in any of the five services.
- Responses are camelCase, confirmed against the live register call: `{"token":"...","userId":"...","role":"Teacher"}`. This corrected an initial assumption that ASP.NET's PascalCase property names would appear on the wire.
- `GET /api/profile` returns `NotFound("Profile not found.")` when no row exists, and `PUT /api/profile` constructs the profile on first save. `UserProfileController.cs:62`.
- `PUT /api/profile` returns validation failures as `BadRequest(validationResult.Errors)`, a bare JSON array of strings, at `UserProfileController.cs:74`.
- `JwtTokenService.cs` hardcodes an 8 hour expiry.
- Registration does not create a UserService profile. The services own separate databases.

**Skill and tooling discovery**

`design-system/SKILL.md` declares `name: quiztin-design` and `user-invocable: true`. It references `guidelines/`, `components/`, and `ui_kits/` directories that do not exist in the export.

A search for existing agent skills or MCP servers covering Tailwind v4, React Router v7, or TanStack Query found nothing credible. There is no official skill or server for any of the three. The only real result is a community maintained, unofficial TanStack skills repository. The honest conclusion is that the signal here is weak and none of it is needed.

## References

**Project sources**

- `context/foundation.md` §0 (breadth first build approach), §7 #5 (React and Vite, Next.js rejected), #14 (Guid identity from the JWT `NameIdentifier` claim), #16 (one YARP gateway as the single frontend origin, CORS configured there)
- `context/security.md`: student academic data is the protected asset, and secrets never enter git or logs
- `context/ui-rules.md`: semantic tokens only, the eight required interactive states, accessibility to WCAG AA
- `context/ui-registry.md`: the 18 components, all currently planned
- `docs/uc14-ui-ux-brief.md`: the Manage Profile layout, role aware fields, and validation behaviour
- `context/code-standards.md`: framework types stay out of the domain and application layers
- `CLAUDE.md`: tenant scope comes from the authenticated `Guid UserId`, never the client

**Practices and standards**

- OWASP guidance on browser session storage: an `HttpOnly` cookie is not reachable by script, and `localStorage` is.
- Refresh token rotation with reuse detection, as described in the OAuth 2.0 security best current practice, including revoking the whole token family when a rotated token is replayed.
- Same origin proxying in development as the standard alternative to configuring CORS per service.
- Hashing high entropy bearer tokens with a fast digest rather than a password hash, because the input already carries full entropy and the cost is paid on every request.

**Links**

- TanStack Skills, community maintained and unofficial: https://github.com/tanstack-skills/tanstack-skills
