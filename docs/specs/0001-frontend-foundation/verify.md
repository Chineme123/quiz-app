# 0001. Verification

How to prove this spec is satisfied. There is no mock layer, so everything below runs against the real backend. Each step names the acceptance criteria from [index.md](index.md) that it proves.

> **Rewritten 2026-07-24 for the single-host architecture.** The original steps targeted the five-microservice backend (`src/Services/AuthService` on port 5005, `src/Services/UserService` on 5079, `src/Services/QuizService` on 5224, a separate `authdb`). Spec 0007 folded all of that into **one `Quiztin.Api` host** on port **8080**, over one `quiztin` database with an `identity` schema (Auth + User merged) and a `quiz` schema. The auth API surface (`/api/auth/*`, `/api/profile`) is unchanged, so the acceptance criteria and their assertions are the same — only the host, ports, and database changed. The old microservice script is preserved in git history if you need it.

## Bring the stack up

The canonical local run is Docker (foundation §7 #29): one Postgres, one app.

```bash
cp .env.example .env
docker compose up -d          # postgres, then the app on :8080
docker compose ps             # both healthy; the app waits on the DB health check
```

The app runs `Development` by default here, so the host migrates both schemas on startup (`RUN_MIGRATIONS_ON_STARTUP=true`) and seeds the demo data (teacher `teacher@quiztin.dev`, student `student@quiztin.dev`). Everything is same-origin on `:8080` — the host serves the SPA at `/` and the API under `/api`, so the backend checks below hit `:8080` directly with no proxy.

For a backend-only loop you can skip Docker for the app and run the host on the host machine:

```bash
export DOTNET_ROOT="$HOME/.dotnet"; export PATH="$HOME/.dotnet:$PATH"
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=quiztin;Username=postgres;Password=postgres"
export JwtSettings__Secret="$(openssl rand -base64 48)"
export ASPNETCORE_ENVIRONMENT=Development   # migrate + seed on startup
dotnet run --project src/Quiztin.Api        # :8080
```

## Backend: the auth session

Task 1 of the build plan is verifiable on its own with curl. The refresh cookie's path is `/api/auth`, so keep it in a jar.

```bash
BASE=http://localhost:8080/api/auth
JAR=$(mktemp); EMAIL="verify-$RANDOM@quiztin.test"

# AC-1  register returns {token,userId,role} and sets an HttpOnly, Path=/api/auth, SameSite=Lax cookie
curl -i -c "$JAR" -X POST "$BASE/register" -H 'Content-Type: application/json' \
  -d "{\"email\":\"$EMAIL\",\"password\":\"Passw0rd!\",\"role\":\"Teacher\"}"
OLD=$(grep quiztin_rt "$JAR" | awk '{print $NF}')
```

Expect 200, a body of `{"token":"…","userId":"…","role":"Teacher"}`, and `Set-Cookie: quiztin_rt=…; path=/api/auth; httponly; samesite=lax`. **AC-1.**

```bash
# AC-5  decode the access token's exp: ~900s out (15 min), not 8h. Then it validates on a
#       protected endpoint in EACH module (Identity and Assessment), proving cross-module auth:
TOKEN=…            # the token from the register body
curl -i localhost:8080/api/profile          -H "Authorization: Bearer $TOKEN"   # 404, not 401 (Identity)
curl -i localhost:8080/api/quizzes/available -H "Authorization: Bearer $TOKEN"   # 200/empty, not 401 (Assessment)
```

A 404 (no profile row yet) and a 200 both prove the token validated. A 401 would mean the shorter lifetime or the claims broke validation. **AC-5.**

```bash
# AC-2  refresh rotates the cookie
curl -i -b "$JAR" -c "$JAR" -X POST "$BASE/refresh"
NEW1=$(grep quiztin_rt "$JAR" | awk '{print $NF}')
test "$OLD" != "$NEW1" && echo "rotated"

# AC-3a  replay the just-rotated token INSIDE the grace window (10s): a successor, no family revoke.
curl -i -X POST "$BASE/refresh" --cookie "quiztin_rt=$OLD"    # expect 200, and NO Set-Cookie

# AC-3b  the same token once the grace window closes: the family dies.
sleep 11
curl -i -X POST "$BASE/refresh" --cookie "quiztin_rt=$OLD"    # expect 401
curl -i -X POST "$BASE/refresh" --cookie "quiztin_rt=$NEW1"   # expect 401, the family is revoked

# AC-4  logout revokes and clears, and is idempotent
curl -i -c "$JAR" -X POST "$BASE/login" -H 'Content-Type: application/json' \
  -d "{\"email\":\"$EMAIL\",\"password\":\"Passw0rd!\"}"
LIVE=$(grep quiztin_rt "$JAR" | awk '{print $NF}')
curl -i -b "$JAR" -X POST "$BASE/logout"                      # expect 204
curl -i -X POST "$BASE/refresh" --cookie "quiztin_rt=$LIVE"   # expect 401
curl -i -X POST "$BASE/logout"                                # expect 204 again (idempotent)
```

**AC-2, AC-3, AC-4.** Then confirm no raw token was persisted — every stored row is a 64-char hex digest (a raw token is 43 chars of base64url):

```bash
docker compose exec postgres psql -U postgres -d quiztin -tAc \
  "select count(*) filter (where \"TokenHash\" ~ '^[0-9a-f]{64}\$') || ' of ' || count(*) from identity.\"RefreshTokens\";"
```

The timing-dependent rules are also covered by unit tests, which curl proves poorly:

```bash
dotnet test tests/Quiztin.Modules.Identity.Tests   # the old AuthService.Tests, merged into the Identity module
```

## The app

Run the SPA dev server; it proxies `/api` to the host on `:8080`.

```bash
cd frontend && npm ci && npm run dev     # http://localhost:5173
```

- **Session survives a reload, nothing durable stored.** Register through the UI. DevTools → Application: the `quiztin_rt` cookie is `HttpOnly`; `localStorage`/`sessionStorage` hold no credential. Hard reload: still signed in, and the network tab shows one `POST /api/auth/refresh` with a changed cookie. **AC-7**, **AC-2** from the client side.
- **Two tabs do not sign each other out.** Open the app in two tabs at the same moment. Both end signed in, and only one issued a refresh (`BroadcastChannel` coordinates them). **AC-3**, **AC-14.**
- **A 401 recovers silently.** Revoke the in-memory access token (a dev hook, or wait 15 min) and trigger a profile fetch: one 401, one refresh, one retry that succeeds. Then revoke the refresh server-side and repeat: the app signs out cleanly, no loop. **AC-14.**
- **Sign out.** The cookie clears, the app returns to `/sign-in`, and a manual refresh call returns 401. **AC-4.**
- **The route guard.** In a fresh private window, go straight to `/profile` → redirected to `/sign-in`. Sign in → you land on `/profile`, not a default page. **AC-9.**
- **Manage Profile, both roles.** Register a new Student. Manage Profile shows an empty form with no error (the `GET /api/profile` 404 is the first-time state), shows Academic Level and not Instructor Type. Save → a quiet confirmation, no redirect, values survive a reload. Repeat as a Teacher: Instructor Type appears, Academic Level does not. **AC-10**, **AC-13.**
- **Validation keeps your work.** Fill the whole form, clear Display Name, save → focus moves to Display Name, an inline error appears, every other field still holds what you typed. **AC-11.**
- **Server errors land on the right field.** Clear the role-specific field so `PUT /api/profile` returns its string array → the message appears beneath Academic Level or Instructor Type, not in a generic banner. **AC-12.**

## Styling and accessibility

- **Tokens only.** No raw hex in application code. A primary button's computed `background-color` traces to `var(--primary)`, not a literal. **AC-6.**
- **Preflight is not loaded.** The compiled stylesheet has no Tailwind reset, and bordered elements still show borders (the `border-style:solid` fix). **AC-6.**
- **Focus and motion.** Tab through sign-in, the header profile menu, and the whole profile form: a visible focus ring at every stop, nothing mouse-only, tab order follows visual order. Enable OS reduced motion → no press scale, hover transform, or transition. **AC-6**, **AC-15.**
- **Targets.** Every button, input, and menu item is ≥44px on its smallest side. **AC-15.**
- **Automated floor.** `npm run test` passes, including the `vitest-axe` assertions on the primitives, the profile error-mapping unit test, and the guard redirect test. **AC-6**, **AC-11**, **AC-12**, **AC-15.**

## What this does not cover

Spec 0002 added the frontend CI job, so a green CI badge now does cover `frontend/` lint + type-check + test + build. CI still does not run the live backend curl script above — `dotnet test` covers the rotation rules through the Identity module tests, but not the cookie flags or the HTTP status codes, which is what the curl steps are for.
