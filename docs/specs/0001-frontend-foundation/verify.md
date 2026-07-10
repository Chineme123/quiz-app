# 0001. Verification

How to prove this spec is satisfied. There is no mock layer, so everything below runs against the real services. Each step names the acceptance criteria from [index.md](index.md) that it proves.

## Bring the stack up

```bash
# 1. Postgres
docker compose up -d postgres

# 2. One signing secret, shared by all three services.
export JwtSettings__Secret="$(openssl rand -base64 48)"

# 3. Migrations (AuthService gains a new one in build plan task 1)
dotnet ef database update -p src/Services/AuthService/AuthService.Infrastructure -s src/Services/AuthService/AuthService.API
dotnet ef database update -p src/Services/UserService/UserService.Infrastructure -s src/Services/UserService/UserService.API

# 4. The three real services, on the http profile.
#    The http profile matters: UseHttpsRedirection is on, and the https profile
#    means self signed certificate pain in the proxy for no benefit.
dotnet run --project src/Services/AuthService/AuthService.API --launch-profile http   # 5005
dotnet run --project src/Services/UserService/UserService.API --launch-profile http   # 5079
dotnet run --project src/Services/QuizService/QuizService.API --launch-profile http   # 5224

# 5. The app
cd frontend && npm run dev
```

## Backend, before any frontend exists

Task 1 of the build plan is verifiable on its own with curl. Do this first, because the app cannot boot until it passes.

```bash
# AC-1: register sets the cookie and keeps its response body.
curl -i -c jar.txt -X POST localhost:5005/api/auth/register \
  -H 'Content-Type: application/json' \
  -d '{"email":"t@quiztin.test","password":"Passw0rd!","role":"Teacher"}'
```

Expect a 200, a body of `{"token":"...","userId":"...","role":"Teacher"}`, and a `Set-Cookie: quiztin_rt=...; path=/api/auth; httponly; samesite=lax`. **AC-1.**

```bash
# AC-2: refresh rotates. Keep the old cookie value before you call it.
OLD=$(grep quiztin_rt jar.txt | awk '{print $NF}')
curl -i -b jar.txt -c jar.txt -X POST localhost:5005/api/auth/refresh
NEW=$(grep quiztin_rt jar.txt | awk '{print $NF}')
test "$OLD" != "$NEW" && echo "rotated"
```

Expect a 200, a new token, and a changed cookie value. **AC-2.**

```bash
# AC-3: replaying the old value past the grace window revokes the family.
sleep 15
curl -i -X POST localhost:5005/api/auth/refresh --cookie "quiztin_rt=$OLD"   # expect 401
curl -i -b jar.txt -X POST localhost:5005/api/auth/refresh                   # expect 401, the family is dead
```

Both must return 401. The second is the important one: a replay does not just fail, it kills the session. **AC-3.**

Then repeat the rotation without the `sleep`, replaying `$OLD` immediately. Within the grace window it must return the successor rather than revoke the family. This is the case that keeps two tabs alive. **AC-3.**

```bash
# AC-4: logout.
curl -i -b jar.txt -c jar.txt -X POST localhost:5005/api/auth/logout   # expect 204, cookie cleared
curl -i -b jar.txt -X POST localhost:5005/api/auth/refresh             # expect 401
```

**AC-4.**

```bash
# AC-5: the access token is short lived, and other services still accept it.
# Decode the exp claim and check it is roughly 15 minutes out, then:
curl -i localhost:5079/api/profile -H "Authorization: Bearer $TOKEN"   # expect 404, not 401
```

A 404 proves UserService validated the token and simply has no profile row yet. A 401 would mean the shorter lifetime or the claims broke validation. **AC-5.**

## The app

**Session survives a reload, and nothing durable is stored.** Register through the UI. Open developer tools. Application storage shows the `quiztin_rt` cookie flagged `HttpOnly`, and `localStorage` and `sessionStorage` hold no credential. Hard reload the page. You stay signed in, and the network tab shows one `POST /api/auth/refresh` with a changed cookie value. **AC-7**, and **AC-2** again from the client side.

**Two tabs do not sign each other out.** This is the step that catches the rotation race, and it is the one most likely to fail. Open the app in two tabs at the same moment, so both boot and both present the same cookie. Both must end up signed in. Then check the network tab: only one tab should have issued a refresh, because `BroadcastChannel` coordinates them. **AC-3**, **AC-14.**

**A 401 recovers silently.** With the app open, revoke the access token in memory (a development only hook, or wait 15 minutes). Trigger a profile fetch. The network tab shows the request fail with 401, one refresh, and one retry that succeeds. Then revoke the refresh token server side and repeat: the app signs out cleanly rather than looping. Exactly one refresh and one retry, never a loop. **AC-14.**

**Sign out.** The cookie is cleared, the app returns to `/sign-in`, and a manual refresh call returns 401. **AC-4.**

**The route guard.** In a fresh private window, navigate straight to `/profile`. You are redirected to `/sign-in`. Sign in. You land on `/profile`, not on a default page. **AC-9.**

**Manage Profile, both roles.** Register a brand new Student. Open Manage Profile: the form is empty and shows no error, because `GET /api/profile` returned 404 and that is the first time state. It shows Academic Level and not Instructor Type. Save. A quiet confirmation appears, the page does not redirect, and the values survive a reload. Repeat with a Teacher and confirm Instructor Type appears and Academic Level does not. **AC-10**, **AC-13.**

**Validation keeps your work.** Fill in the whole form, then clear Display Name and save. Focus moves to Display Name, an inline error appears beneath it, and every other field still holds what you typed. **AC-11.**

**Server errors land on the right field.** Force a server side failure by clearing the role specific field, so `PUT /api/profile` returns its string array. The message appears beneath Academic Level or Instructor Type, not in a generic banner. **AC-12.**

## Styling and accessibility

**Tokens only.** Search application code for a raw hex value and find none. Inspect any primary button in developer tools: its computed `background-color` must trace back to `var(--primary)`, not to a literal. **AC-6.**

**Preflight is not loaded.** Confirm the compiled stylesheet contains no Tailwind reset, and that bordered elements still show their borders, which is the trap this creates. **AC-6.**

**Focus and motion.** Tab through sign in, the header profile menu, and the whole profile form. Every stop shows a visible focus ring, nothing is reachable only by mouse, and the tab order follows the visual order. Then enable reduced motion at the operating system level and confirm no press scale, no hover transform, and no transition runs. **AC-6**, **AC-15.**

**Targets.** Inspect each button, input, and menu item. The smallest side is at least 44 pixels. **AC-15.**

**Automated floor.** `npm run test` passes, including the `vitest-axe` assertions on the primitives, the profile error mapping unit test, and the guard redirect test. **AC-6**, **AC-11**, **AC-12**, **AC-15.**

## Backend, task 1: the script that actually ran (2026-07-10, all passing)

This supersedes the prose sketch above for the backend, and it corrects one thing. The
grace window does **not** return the successor token, because the server only ever holds
its hash. It returns a new access token and leaves the cookie untouched. `AC-3` still needs
rewording by `/architect`; the behaviour below is the correct one.

Run it with the service on the http profile and Postgres up. It is self contained and exits
non zero on any failure.

```bash
export JwtSettings__Secret="$(openssl rand -base64 48)"
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=authdb;Username=postgres;Password=postgres"
export ASPNETCORE_URLS="http://localhost:5005"
export ASPNETCORE_ENVIRONMENT=Production   # appsettings.Development.json points at the docker host
dotnet run --project src/Services/AuthService/AuthService.API &

BASE=http://localhost:5005/api/auth
JAR=$(mktemp); EMAIL="verify-$RANDOM@quiztin.test"

# AC-1  register returns {token,userId,role} and sets an HttpOnly, Path=/api/auth, SameSite=Lax cookie
curl -i -c "$JAR" -X POST "$BASE/register" -H 'Content-Type: application/json' \
  -d "{\"email\":\"$EMAIL\",\"password\":\"Passw0rd!\",\"role\":\"Teacher\"}"
OLD=$(grep quiztin_rt "$JAR" | awk '{print $NF}')

# AC-5  decode the access token: exp should be about 900 seconds out, not 8 hours

# AC-2  refresh rotates the cookie
curl -i -b "$JAR" -c "$JAR" -X POST "$BASE/refresh"
NEW1=$(grep quiztin_rt "$JAR" | awk '{print $NF}')
test "$OLD" != "$NEW1"          # the cookie value must have changed

# AC-3a  replay the just rotated token INSIDE the grace window
curl -i -X POST "$BASE/refresh" --cookie "quiztin_rt=$OLD"
#   expect 200, and expect NO Set-Cookie header (the browser already holds the successor)

# AC-3b  the same token once the grace window closes
sleep 11
curl -i -X POST "$BASE/refresh" --cookie "quiztin_rt=$OLD"    # expect 401
curl -i -X POST "$BASE/refresh" --cookie "quiztin_rt=$NEW1"   # expect 401, the family is revoked

# AC-4  logout revokes and clears, and is idempotent
curl -i -c "$JAR" -X POST "$BASE/login" -H 'Content-Type: application/json' \
  -d "{\"email\":\"$EMAIL\",\"password\":\"Passw0rd!\"}"
LIVE=$(grep quiztin_rt "$JAR" | awk '{print $NF}')
curl -i -b "$JAR" -X POST "$BASE/logout"                      # expect 204
curl -i -X POST "$BASE/refresh" --cookie "quiztin_rt=$LIVE"   # expect 401
curl -i -X POST "$BASE/logout"                                # expect 204 again
```

Then confirm nothing sensitive was persisted:

```bash
psql -h localhost -U postgres -d authdb -tAc \
  "select count(*) filter (where \"TokenHash\" ~ '^[0-9a-f]{64}\$') || ' of ' || count(*) from \"RefreshTokens\";"
# every row must be a 64 character hex digest. A raw token is 43 characters of base64url.
```

Unit coverage for the same rules, which curl proves poorly because they depend on timing:

```bash
dotnet test src/Services/AuthService/AuthService.Tests   # 27 tests
```

### Acceptance criteria coverage, backend

- **AC-1** covered by the register call and its `Set-Cookie` assertions.
- **AC-2** covered by the rotation check that `$OLD` and `$NEW1` differ.
- **AC-3** covered by AC-3a (grace, 200 and no `Set-Cookie`) and AC-3b (replay, 401 plus family revocation), and by `RefreshTokenServiceTests`.
- **AC-4** covered by the logout sequence, including the idempotent second call.
- **AC-5** covered by decoding `exp` from the access token.

## What this does not cover

CI does not run any of the frontend checks. Until the Node job in Follow-up exists, a green CI badge means the .NET solution built and its tests passed, and says nothing at all about `frontend/`.

CI does not run the live backend script above either. It runs `dotnet test`, which covers the
rotation rules through `AuthService.Tests` but not the cookie flags or the HTTP status codes.
