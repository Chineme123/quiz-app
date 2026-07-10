# Quiztin — Security

> **Status:** v1. Data-handling authority for this project's crown-jewel concern: **student academic data (answers, scores) that is sent to a third-party LLM (Anthropic Claude) for feedback**, plus the app's own secrets (DB credentials, JWT signing key, Claude API key).
> **When this file and any other file conflict on a data-handling question, this file wins.** Code-level enforcement lives in `code-standards.md`, which defers here for policy. For *why* the LLM is in the product at all, see `foundation.md` §7 #6/#17.

**Status key:** ✅ enforced · ⬜ to enforce (not yet true in code) · ⚠️ current violation to fix.

## §1 The model / external AI provider (Anthropic Claude)
- **API only.** Reach Claude through the Anthropic **API**, never a consumer surface. ⬜
- **Model output is untrusted input.** Generated questions and feedback are validated/sanitized before persistence or display — treat them like user input (no blind HTML render, enforce the question schema). ⬜
- **Provider trust:** rely on Anthropic's API data policy (API inputs/outputs are not used to train models by default). Do not send anything you wouldn't be comfortable leaving the classroom boundary.
- **Every call has a deterministic fallback** (foundation §7 #6) — a provider outage degrades the feature, never breaks the loop.

## §2 Data minimization — send only what the task needs
The core rule. Minimize what crosses the boundary to Claude:
- **Question generation:** send the teacher's topic, difficulty, and any source materials the teacher chose to attach. This is the teacher's own content, teacher-initiated. ⬜
- **Per-question feedback:** send only `{ question text, correct answer, the student's submitted answer }` for the one question. ⬜
- **Never send:** student identity (name, email, `UserId`), other students' data, the full attempt/history, classroom or teacher identifiers, or anything not needed to generate feedback for that one question. Feedback is **pseudonymous by construction** — the model never learns *who* answered. ⬜
- **Store derived, not raw:** persist the resulting feedback text; do not store raw model request/response transcripts beyond what the feature needs.

## §3 Secrets & credentials (the crown jewels)
- **Env vars / user-secrets only.** DB connection string, JWT signing key, and Claude API key come from configuration/environment — never hardcoded, never committed, never sent to the frontend. ✅ (JWT key + DB passwords done; Claude key pending its integration)
- ✅ **Resolved 2026-07-09:** the JWT signing key (was hardcoded in `QuizService/Program.cs` and duplicated in `appsettings.json`) now comes from configuration — a gitignored `.env` for docker-compose, user-secrets for local dev — with an empty value in committed config. The key was **rotated** to a new random value and the old value **purged from git history**. DB passwords likewise moved to `${POSTGRES_PASSWORD}`.
- **Least exposure:** the Claude API key lives only in the service(s) that call Claude (QuizService for generation + feedback), not spread across all five.

## §4 App auth / sessions
- **JWT (HS256), issued only by AuthService** (foundation §7 #15); all services validate against a single shared issuer + audience + signing key. ✅ **Resolved (PR #19):** AuthService mints tokens; issuer and audience are both `quiztin` everywhere; the signing key is one shared `JwtSettings__Secret` from config. (Old drift: audiences once disagreed, `quiz-app` vs `http://localhost:5000`.)
- **Identity is the token, not the client.** Every request's principal is the `Guid UserId` from the JWT `NameIdentifier` claim (foundation §7 #14). ✅ **Resolved (PR #19):** the hardcoded `"teacher-1"` and `Guid.Empty` fallbacks are gone; QuizService controllers are `[Authorize]` and read the Guid from the claim; a missing/invalid identity is `401`/`403`, never a default user.
- **Session model: in-memory access token + rotating refresh cookie** (foundation §7 #26, spec 0001, built PR #23). ✅ The short-lived access token (`AuthTokens:AccessTokenMinutes`, default 15) is held only in browser memory. The long-lived credential is a refresh token in an `HttpOnly`, `SameSite=Lax`, `Path=/api/auth`, host-only cookie (`Secure` from config: false only for local http). This keeps a durable credential unreachable by any injected script — an XSS payload cannot exfiltrate an `HttpOnly` cookie, and a stolen 15-minute access token expires fast.
  - **At rest, only a SHA-256 hash of the refresh token is stored** (`RefreshTokens.TokenHash`), never the raw value; the raw token exists once, in the `Set-Cookie` response, and is never persisted or logged (§6). A fast digest is correct because the token is already full-entropy random.
  - **Rotation with reuse detection:** each use rotates the token within its `SessionId` family; replaying an already-rotated token revokes the whole family (theft response). A replay inside a short grace window (`AuthTokens:RotationGraceSeconds`, default 10) is treated as a benign two-tab race, not theft: the server mints a fresh access token without rotating or re-setting the cookie. `POST /api/auth/refresh` exchanges the cookie for a new access token; `POST /api/auth/logout` revokes and clears it.
  - AuthService itself wires **no** JWT middleware and **no** CORS: register/login are anonymous, refresh/logout authenticate via the cookie, and the SPA reaches it same-origin through the Vite proxy now / the gateway later (foundation §7 #27). This is deliberate, not a gap.
- **Tenant isolation is a security boundary:** every classroom/quiz/attempt query is scoped by that principal. An unscoped query on a tenant table is a security bug (`code-standards.md` §5).

## §5 Inbound trust boundaries
- The **gateway is the only public origin**; services trust requests arriving through it but still validate the JWT (don't assume the gateway sanitized identity). ⬜
- No third-party inbound webhooks in v1. If one is added (e.g. a payment or LMS callback), it must validate a signature before trusting anything — add the rule here first.

## §6 Logging & error hygiene
- **Never log:** the Claude API key, JWT signing key, DB password, raw JWTs, student answers, generated feedback bodies, or prompts/responses to the model.
- **Do log:** enough to debug — request IDs, user id (the Guid is fine), error types and context prefixes — without the sensitive payload.
- ⚠️ **Current gap:** `UserProfileController` catches `Exception` and returns 500 with a `// Log error` comment that logs nothing. Error handling must actually log (structured, no sensitive fields) — see `code-standards.md` §8.

## §7 Sensitive-data categories
- **Student academic performance** (scores, answers, feedback) is the sensitive category here. It stays inside the classroom boundary: visible to the student who owns it and the teacher who owns the classroom — never to other students, never to the model with identity attached. Aggregate/classroom views (UC10) show performance within the teacher's own classroom only.
- No health/financial/messaging data is handled. If that changes, extend this section before writing the code.
