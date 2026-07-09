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
- **Env vars / user-secrets only.** DB connection string, JWT signing key, and Claude API key come from configuration/environment — never hardcoded, never committed, never sent to the frontend. ⬜
- ⚠️ **Current violations to fix (Layer 0):** the JWT signing key is hardcoded in `QuizService/Program.cs` (`"SuperSecretKey…"`) and duplicated in UserService `appsettings.json`; the Postgres/DB password is committed. These move to secrets as part of the auth/identity must-fix.
- **Least exposure:** the Claude API key lives only in the service(s) that call Claude (QuizService for generation + feedback), not spread across all five.

## §4 App auth / sessions
- **JWT (HS256), issued only by AuthService** (foundation §7 #15); all services validate against a single shared issuer + audience + signing key. ⚠️ Today audiences disagree (`quiz-app` vs `http://localhost:5000`) and no service issues tokens — fix in v1.
- **Identity is the token, not the client.** Every request's principal is the `Guid UserId` from the JWT `NameIdentifier` claim (foundation §7 #14). ⚠️ Remove all hardcoded/fallback IDs (`"teacher-1"`, `Guid.Empty` fallbacks); a missing/invalid identity is `401`, never a default user.
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
