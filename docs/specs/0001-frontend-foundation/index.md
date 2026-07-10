# 0001. Frontend foundation, and the auth session it depends on

**Date**: 2026-07-10
**Status**: Proposed

## Summary

Quiztin has a backend, a design system, and no frontend. This spec settles the shape of the React app in `frontend/`, and it settles one backend change the app cannot work without: a refresh endpoint, so the browser never has to store a long lived token where a script could read it. It covers only the screens that have real endpoints behind them today, which are sign in, register, and Manage Profile. Everything else waits for the dashboard slice.

## Requirements

**User stories**

- As a student or teacher, I want to sign in and stay signed in when I reload the page, so I am not asked for my password constantly.
- As a signed in user, I want my session to be safe even if a script runs on the page, so a stolen token cannot be replayed for weeks.
- As a student or teacher, I want to fill in my profile and see only the fields that apply to my role, so the form does not ask me irrelevant questions.
- As a user who makes a mistake in a form, I want to be told what is wrong without losing everything else I typed.
- As a keyboard user or a screen reader user, I want every control reachable and clearly focused.

**Acceptance criteria**

- **AC-1**: `POST /api/auth/register` and `POST /api/auth/login` return their current response bodies unchanged, and additionally set an `HttpOnly` cookie named `quiztin_rt` scoped to path `/api/auth`.
- **AC-2**: `POST /api/auth/refresh` with a valid cookie returns a new access token and replaces the cookie. The previous refresh token stops working.
- **AC-3**: Presenting a refresh token that was already rotated revokes every token in its session family and returns 401. A token replaced within the grace window (10 seconds) instead returns its successor, so two tabs racing on load do not sign each other out.
- **AC-4**: `POST /api/auth/logout` revokes the token and clears the cookie. A later `refresh` returns 401.
- **AC-5**: Access token lifetime comes from configuration, defaults to 15 minutes, and tokens still validate against UserService and QuizService unchanged.
- **AC-6**: Every colour, radius, shadow, and font in the app resolves to a design system custom property. No raw hex appears in application code. Tailwind Preflight is not loaded, the focus ring is visible on every focusable control, and `prefers-reduced-motion` suppresses transforms and transitions.
- **AC-7**: The access token exists only in a JavaScript variable. `localStorage` and `sessionStorage` are empty of credentials. A full page reload leaves the user signed in, by exchanging the cookie at `/api/auth/refresh`.
- **AC-8**: Sign in and register work end to end. Every interactive control has default, hover, press, disabled, and focus states, and each form has loading and error states. Copy is sentence case with no emoji.
- **AC-9**: An unauthenticated request for `/profile` redirects to `/sign-in`, and signing in returns the user to `/profile`.
- **AC-10**: Manage Profile loads from `GET /api/profile`, treats 404 as the first time empty form rather than an error, and shows Academic Level to students and Instructor Type to teachers, never both.
- **AC-11**: Submitting an invalid form keeps the value of every other field, and moves focus to the first field in error.
- **AC-12**: The string array returned by `PUT /api/profile` on validation failure is mapped back to the field it belongs to and shown beneath that field.
- **AC-13**: A successful save shows a quiet confirmation, does not redirect, and leaves the form populated with the saved values.
- **AC-14**: A 401 on any authenticated request triggers exactly one silent refresh and one retry. If the refresh fails, the user is signed out. Concurrent 401s share a single refresh call, and every open tab reacts to the result.
- **AC-15**: All three screens are fully operable by keyboard alone, and every interactive target is at least 44 pixels on its smallest side.

## Decision

**Chosen option**: Option 2: A token bound Tailwind SPA behind a same origin proxy, with a rotating refresh cookie.

Build `frontend/` as a React and TypeScript single page app compiled by Vite, styled by Tailwind v4 whose theme is bound by reference to the existing design system custom properties, talking to the services through a same origin proxy, and holding its access token only in memory while a rotating refresh token lives in an `HttpOnly` cookie.

**Implementation skills**: `quiztin-design` (local export, installed at `.claude/skills/quiztin-design/`)

## Proposed stack

| Layer | Choice | Reason |
|---|---|---|
| Language | TypeScript, strict | The API surface is the risky boundary, and types plus Zod catch drift there before it reaches a screen |
| Build tool | Vite 8 | Already locked in foundation §7 #5, and its dev proxy is what makes a browser able to reach this backend at all |
| UI | React 19 | Locked in foundation §7 #5 |
| Routing | React Router v7, `createBrowserRouter`, loaders and actions unused | Gives nested routes and error boundaries without adopting framework mode, whose Vite plugin and server rendering are the machinery this project rejected with Next.js |
| Server state | TanStack Query v5 | Caching, retry policy, and request deduplication are exactly the problems a quiz app has, and hand rolling them is where bugs live |
| Forms | React Hook Form with Zod v4 resolvers | UC14 demands inline errors that preserve other fields and focus the first error, which is this library's default behaviour |
| Styling | Tailwind v4 with `@theme inline`, Preflight not loaded | Binds utilities to the token variables by reference, so the tokens only rule is enforced by the compiler instead of by review |
| Components | Authored in this repo from `design-system/tokens/*.css` and `design-system/HANDOFF.md` | The export ships no React source, only a compiled browser global that Vite cannot import |
| Icons | `@phosphor-icons/react` | The icon set the design system was drawn against |
| Session | Access token in memory, rotating refresh token in an `HttpOnly` cookie | The only shape where a script on the page cannot steal a durable credential |
| Dev origin | Vite dev server proxy | There is no CORS anywhere in the backend, so the app must be same origin with the API |
| Production origin | The YARP gateway, foundation §7 #16 | Same origin by the same logic, no client change needed |
| Testing | Vitest, Testing Library, `vitest-axe` | Matches the Vite toolchain, and the accessibility rules need an automated floor |
| Lint | ESLint flat config, `eslint-plugin-jsx-a11y`, Prettier | The accessibility rules are a stated requirement, so they belong in the linter |
| Package manager | npm | No new tooling for a solo developer; the repo has no lockfile to preserve |

Exact minor versions are pinned at scaffold time by querying the registry, not copied from this document.

## Session design (the auth refresh)

**Data model sketch**

New table `RefreshTokens` in `authdb`:

| Field | Type | Notes |
|---|---|---|
| `Id` | `uuid` | primary key |
| `UserId` | `uuid` | indexed |
| `SessionId` | `uuid` | constant across one rotation chain, so a family can be revoked together, indexed |
| `TokenHash` | `text` | SHA 256 of the raw token, unique index. The raw value is never stored |
| `ExpiresAt` | `timestamptz` | |
| `CreatedAt` | `timestamptz` | |
| `RevokedAt` | `timestamptz` | null while live |
| `ReplacedByTokenId` | `uuid` | null until rotated, points at the successor |

The raw token is 32 bytes from `RandomNumberGenerator`, base64url encoded. It is already high entropy, so a fast hash is the correct choice here and a slow password hash such as PBKDF2 would only cost latency on every request.

**State transitions**

A refresh token moves `live` to `rotated` (revoked, with a successor) or `live` to `revoked` (logout, or family revocation). It never returns to `live`.

**API surface**

| Endpoint | Method | Key inputs | Key outputs | Auth | Key errors |
|---|---|---|---|---|---|
| `/api/auth/register` | POST | email, password, role | token, userId, role, plus `Set-Cookie` | anonymous | 409, 400 |
| `/api/auth/login` | POST | email, password | token, userId, role, plus `Set-Cookie` | anonymous | 401 |
| `/api/auth/refresh` | POST | the `quiztin_rt` cookie | token, userId, role, plus a rotated `Set-Cookie` | the cookie itself | 401 |
| `/api/auth/logout` | POST | the `quiztin_rt` cookie | no content, cookie cleared | the cookie itself | none, it is idempotent |

`refresh` returns `userId` and `role` alongside the token on purpose. It means the client never has to decode a JWT, so the app takes no JWT parsing dependency and cannot be tricked by an unverified payload.

`AuthController` owns every read and write of the cookie. The application service returns the raw token as a value and never touches `HttpContext`, which keeps ASP.NET out of the application layer, per `code-standards.md`.

**Key invariants**

- The raw refresh token is never written to the database, never logged, and never returned in a response body. It travels only in the cookie.
- One live refresh token per session family at any moment.
- Rotation and revocation happen in the same transaction as the lookup, so two concurrent refreshes cannot both succeed.
- Access token claims are unchanged: `NameIdentifier` carries the canonical `Guid`, per foundation §7 #14.

**Security model**

Cookie flags: `HttpOnly`, `SameSite=Lax`, `Path=/api/auth`, `Secure` from configuration, false in local development over plain http and true everywhere else. Host only, with no `Domain` attribute, which is what lets it survive the dev proxy and later the gateway unchanged.

`Path=/api/auth` means the cookie is not attached to profile or quiz requests, so it is never in scope for a request that does not need it.

The threat this design answers: a script injected into the page can read anything in `localStorage`, so a durable token stored there is a durable compromise. It cannot read an `HttpOnly` cookie. The worst it can do is use the session while the page is open, and a 15 minute access token bounds the damage after that.

**Configuration required**

- `JwtSettings__Secret`: the HS256 signing key, shared by Auth, User, and Quiz. Already required, still never committed.
- `AuthTokens__AccessTokenMinutes`: defaults to 15.
- `AuthTokens__RefreshTokenDays`: defaults to 14.
- `AuthTokens__Cookie__Secure`: false in local development, true in production.

**Critical test scenarios**

- Happy path: register, reload the page, land still signed in with a rotated cookie. Verifies **AC-1**, **AC-2**, **AC-7**.
- Failure case: replay a refresh token that was already rotated more than the grace window ago. The family is revoked and every later refresh returns 401. Verifies **AC-3**.
- Concurrency: two tabs load at the same moment with the same cookie. Both end up signed in and neither is signed out. Verifies **AC-3**, **AC-14**.
- Auth and permission: an anonymous browser asks for `/profile` and is redirected to `/sign-in` rather than shown an empty shell. Verifies **AC-9**.
- Domain edge: a brand new account opens Manage Profile, receives 404, and sees an empty form rather than an error. Verifies **AC-10**.

## Build plan

The project build approach is breadth first, foundation §0, which is why the primitives and the client infrastructure are built out as layers before any screen is assembled, rather than dragging one thin thread through every layer. The one hard ordering constraint is that the app boots by calling `/api/auth/refresh`, so that endpoint must exist before the app has an auth layer at all.

1. AuthService refresh and logout: the `RefreshToken` entity, the `AddRefreshTokens` migration, the token service and repository, configuration for lifetimes and cookie flags, and the two controller endpoints. Move the hardcoded `AddHours(8)` in `JwtTokenService` to configuration. Verified with curl before any frontend exists. Satisfies **AC-1**, **AC-2**, **AC-3**, **AC-4**, **AC-5**.
2. Scaffold `frontend/`: Vite, TypeScript, the proxy route map, the Tailwind entry that binds to the tokens, ESLint with the accessibility plugin, Vitest, and Node entries in `.gitignore`. Satisfies **AC-6**.
3. The seven primitives this spec needs, ported from `design-system/HANDOFF.md` with `_ds_bundle.js` as the reference for markup and class names: Icon, Button, Field, TextField, Select, Card, Toast. Each carries all of its required states. Satisfies **AC-6**, **AC-15**.
4. The API client: the typed fetch wrapper, the in memory token store, the single flight refresh with cross tab coordination, the TanStack Query client, and Zod validation at the boundary. Satisfies **AC-7**, **AC-14**.
5. The auth context, the router, the route guard, and the app shell with its header and profile menu. Satisfies **AC-7**, **AC-9**.
6. Sign in and Register. Satisfies **AC-8**.
7. Manage Profile, following `docs/uc14-ui-ux-brief.md`. Satisfies **AC-10**, **AC-11**, **AC-12**, **AC-13**.
8. Tests: accessibility assertions on the primitives, the server error mapping, and the guard redirect. Satisfies **AC-6**, **AC-11**, **AC-12**, **AC-15**.

Verification steps live in `verify.md`.

## Consequences

**Positive**

- No durable credential is reachable by any script on the page, which is the single largest win available in a browser app.
- Binding Tailwind to the tokens by reference makes the tokens only rule mechanical. A developer who types a raw hex has to work around the system to do it.
- Building against real endpoints only means nothing in this spec can be quietly wrong about the backend. There is no mock to disagree with reality.
- The dev proxy and the future gateway present the same origin to the app, so the move to the gateway is a configuration change and not a client rewrite.

**Negative and tradeoffs**

- Rotating refresh tokens add a real failure mode: a race between tabs looks exactly like a stolen token. Two mitigations are required, one on each side, and both must be built or the app will sign users out at random.
- Disabling Preflight means Tailwind's `border` utilities render invisible until a three line rule restores `border-style: solid`. This is a known trap and it is in the build plan.
- Every one of the 18 components must be written by hand. The design system export accelerates nothing but the styling decisions.
- An access token that lives 15 minutes instead of 8 hours means a refresh round trip roughly every 15 minutes per active session. That is the cost of the security posture, and it is the right trade.
- Eleven of the 18 registry components stay unbuilt after this spec. That is deliberate, not an oversight, and `ui-registry.md` will continue to show them as planned.

**Neutral**

- `frontend/` sits outside `QuizApp.sln` and outside CI. A green CI badge will not mean the frontend passed anything until a Node job is added.
- `PUT /api/profile` creates the profile row on first save, so registration deliberately does not provision one. AuthService and UserService keep separate databases.

## Follow-up

- [ ] Add a CI job that installs, lints, type checks, and tests `frontend/`. Until then CI green does not cover the app.
- [ ] Change `PUT /api/profile` to return structured `{field, message}` errors instead of a bare array of strings. The client currently has to match on message prefixes, which breaks the moment anyone rewords a message.
- [ ] Write the YARP gateway spec. When it lands, the cookie flips to `Secure=true` and the client needs no change.
- [x] Install `design-system/SKILL.md` as `.claude/skills/quiztin-design/SKILL.md`. Done. It pointed at `guidelines/`, `components/`, and `ui_kits/` directories that are absent from the export, so the installed copy prunes those references, points at `design-system/` rather than duplicating the tokens, and states plainly that no importable React source exists.
- [ ] Add an avatar upload endpoint. The UC14 brief asks for upload with a preview, and the DTO carries only `AvatarUrl`, so this ships as a URL field.
- [ ] Self host the fonts. They load from the Google Fonts CDN for now, which is a third party request on every page load.
- [ ] Dark mode is not in the design system export. Decide whether it is in scope before component count grows.

## Rationale

The context, the options weighed, and the reasoning live in [rationale.md](rationale.md).
