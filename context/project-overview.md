# Quiztin — Project Overview

> This file **summarizes**. `foundation.md` is the complete, authoritative source — read it for the full reasoning behind every decision below. If this file and `foundation.md` ever disagree, `foundation.md` wins.

## About the project
Quiztin is a classroom quiz web application for academic settings. Teachers create classrooms, author and publish quizzes inside them (by hand or with AI assistance), and review how their class did. Students enrol in classrooms, take the quizzes they're eligible for, get an immediate score, and review each question with AI-generated feedback. It's built as coursework under the Agile Unified Methodology, but with the intent of being a real, demoable product — the bar for "done" is a working end-to-end loop, not a submitted document.

## The problem it solves
Assessment tools are either rigid (fixed question banks, manual grading, no feedback) or generic (no classroom structure, no roster control). Quiztin combines classroom-scoped structure (only enrolled students take a teacher's quizzes) with immediate scoring and **per-question AI feedback** — so a student doesn't just see a number, they see *why* an answer was wrong and how to think about it. That feedback layer is the product's differentiator.

## The apps / surfaces
- **A React + Vite single-page app** (`frontend/`) — the one thing users touch, covering both teacher and student flows.
- **A backend of five .NET microservices** behind a single API gateway. Users never see the services directly; the SPA talks to the gateway.
- (Dev only) Swagger UI per service for testing.

## Core end-to-end flow
The loop the product lives or dies on:
1. Teacher **creates a classroom** (open or invite-only).
2. Student **joins/enrols** in it.
3. Teacher **creates a quiz** — configures it, authors questions manually or via AI generation, reviews, publishes.
4. Enrolled student **takes the quiz** — one attempt at a time; on submit it's scored immediately and each question gets AI feedback.
5. Student **sees their results** — score plus a per-question breakdown with feedback.
6. Teacher **sees classroom results** — how the whole class did.

## Key invariants
- **Enrolment gates taking (FR7):** only students enrolled in a classroom can take its quizzes.
- **Identity is one thing:** every user is a `Guid UserId`, always read from the JWT — never a client-supplied or hardcoded ID.
- **Tenant isolation is a security boundary:** every classroom/quiz/attempt query is scoped by the authenticated principal.
- **One active attempt per student per quiz:** starting a new attempt abandons a prior unfinished one; attempt limits are configurable.
- **The AI degrades, never breaks:** every Claude call has a deterministic fallback.
- **No student PII to the LLM; no secrets in git.**

## Features in scope (v1)
Create classroom (UC2) + join classroom (UC3); create quiz with real AI generation (UC6); take quiz with real scoring + real AI feedback and the attempt-limit/abandon rules (UC8); view my results (UC9); view classroom results (UC10); user profile (UC14); a minimal AuthService issuing real JWTs with end-to-end auth; a YARP API gateway; and the React SPA covering these screens. Plus the Layer-0 repairs the loop depends on (Postgres migration, real scoring contract, framework pin, identity/auth fix). *Reasoning for each lives in `foundation.md` §7–§8.*

## Features out of scope (deferred)
NotificationService; assign-quiz (UC7); quiz metrics/grading-style/export (UC11–13); full profile/account/admin/roles/preferences/activity (UC15–19); practice mode (UC4/UC5); the 4th & 5th question types; full multiple-attempt history/analytics; save-draft; auto-save; a background abandon-sweeper; an event outbox. The final `quiz-trash/` cleanup pass is the last deferred step.

## Target users
Teachers (own classrooms, author quizzes, view class results) and students (enrol, take quizzes, review feedback) in an academic classroom setting. One instructor per classroom. Admin is designed but out of v1.

## Success criteria & stage
**Stage:** early-foundation brownfield — QuizService (create + take) and UserService (profile) are real; Auth/Result/Notification are scaffolds; no frontend yet. **Success (v1):** a student can log in, be enrolled, take a published quiz, get a real score, and review per-question AI feedback; a teacher can create a classroom, author (optionally AI-assisted) and publish a quiz, and see classroom results. Solo developer, ~10 hrs/week, no deadline, Claude-accelerated.

---

Summary only. For the complete picture and the reasoning behind every decision, read `foundation.md`. For the technical shape, see `architecture.md`.
