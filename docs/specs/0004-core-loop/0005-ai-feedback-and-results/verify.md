# Verify: AI feedback and student results · spec 0005 · updated 2026-07-15

_Steps derived from spec 0005 acceptance criteria. `/check verify` runs these; `/test` locks the durable ones. The first core-loop child (umbrella 0004)._

## Setup
- [ ] `docker compose up postgres` (or a local Postgres on 5432), then run QuizService in Development. The seeder logs "Seeded the core loop" and creates a teacher, a classroom, a published quiz (3 question types), a student, and an enrolment. → AC-11
- Seed ids: student `22222222-0000-0000-0000-000000000002`, quiz `44444444-0000-0000-0000-000000000004`. Mint a JWT whose `nameid`/`sub` is the student id, `iss`/`aud` = `quiztin`, signed with `JwtSettings:Secret`.

## Commands (backend)
- [ ] `POST /api/quizzes/44444444-…-004/start` (bearer) → 201 with `{ attemptId }`.
- [ ] `POST /api/attempts/{attemptId}/submit` (bearer, `{ commandId, responses[] }`) → returns fast with a success message; the submit path makes **no** model call. → AC-1
- [ ] `GET /api/attempts/{attemptId}/result` (bearer) → score + per-question breakdown (question text, your answer, correct answer, correct?, points, feedback, feedback source). → AC-8
- [ ] Right after submit, `feedbackStatus` is `Pending` (or already `Ready` for the fast deterministic path); it reaches `Ready`. → AC-7
- [ ] `GET /api/attempts/{another-student's-attempt}/result` → **404** (never reveals another student's work). → AC-9
- [ ] With `Feedback:AiEnabled=false` (default, no key): every answer's feedback source is `Deterministic`; correct → "Nice — that's right.", wrong → "worth another look". → AC-3, AC-4
- [ ] With `Feedback:AiEnabled=true` + `Anthropic:ApiKey` set: wrong answers get AI feedback (source `Ai`) in **one call per attempt**; correct answers stay deterministic (no call). Break the key/network → the whole attempt falls back to deterministic and still reaches `Ready`. → AC-2, AC-4, AC-5
- [ ] No prompts, answers, feedback bodies, or the key appear in the logs. → AC-6
- [ ] Deliver the graded event twice (or re-run the worker on a `Ready` attempt) → no second Claude call, no throw. → AC-2, AC-7

## UI / manual (frontend)
- [ ] Signed in, visit `/results/{attemptId}` → the score and per-question breakdown render; wrong answers show "To review" + the correct answer, correct show "Correct". → AC-8, AC-12
- [ ] While `feedbackStatus` is `Pending` → each feedback block shows "Quiztin is writing your feedback…" and the screen polls the result endpoint until `Ready`, then renders the feedback (AI and deterministic shown the same way). → AC-12
- [ ] Visit `/results/{unknown-or-others-id}` → a calm "we couldn't find that result" state (mirrors the 404). → AC-9
- [ ] `npx vitest run src/features/results` → states and an axe pass are green. Tab the screen: focus visible, structure semantic. → AC-12
- [ ] Full-stack demo: `docker compose up` (all services + gateway), register a user, mint/seed an attempt for them, open the results screen through the gateway origin.

## Acceptance-criteria coverage
- AC-1 submit fast, no model on submit · AC-2 one AI call per attempt, index-mapped · AC-3 correct → deterministic, no call · AC-4 fallback on any failure · AC-5 minimized pseudonymous payload · AC-6 no sensitive logging · AC-7 Pending→Ready, idempotent · AC-8 result breakdown · AC-9 scoped 404 · AC-10 untrusted, length-capped, plain text · AC-11 cold-start seed · AC-12 results screen with polling and the calm voice.
