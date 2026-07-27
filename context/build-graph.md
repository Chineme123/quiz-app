# Quiztin — Build Graph

> A dependency map, not a plan. What depends on what. For *why* anything is built, see `foundation.md`; for what already exists, see `progress-log.md`.
> **⚠️ Superseded in part by [spec 0007](../docs/specs/0007-modular-monolith/index.md) (2026-07-18):** there is no gateway node and no ResultService node any more — one host, two modules (Identity, Assessment), one database. Results live in the Assessment module (no separate `resultdb` projection). Read the nodes below through that lens.

## How to read this file
This is a map of *what-requires-what* — **NOT a timeline, a build plan, or a prescribed order**. The developer builds breadth-first with Claude (foundation §0), picking their own path; this file just says what a node genuinely needs before it can work. **Hard requirement** = cannot build/run without it. **Soft benefit** = easier or nicer with it, possible without.

## Layer 0 — foundational prerequisites (nearly everything needs these)
> ✅ **Layer-0 is complete** (see `progress-log.md`, 2026-07-10) — the items below are built. Kept as the dependency map: features still *require* these, they are just no longer pending.
These were the must-fixes from `foundation.md` §8.
- ✅ **`global.json` + framework pin** to .NET 10; every `.csproj` reconciled (duplicated-header one fixed).
- ✅ **PostgreSQL up** via `docker compose` (runs `postgres:17`; the `mssql`→`postgres` swap is done), connection strings in config/secrets.
- ✅ **Regenerated EF migration** against Postgres, including the attempt-side tables (QuizAttempt/QuizAnswer/Enrollment/ProcessedCommand).
- ✅ **Canonical identity plumbing**: `Guid UserId` from the JWT `NameIdentifier` claim, everywhere; hardcoded IDs removed.
- ✅ **Minimal AuthService** issuing HS256 JWTs; shared issuer/audience/key from secrets.
- 🟡 **API gateway (YARP)** as the single frontend origin (no CORS — same-origin, §7 #27). Spec 0002; a production concern, so the Vite proxy is the dev single-origin today.

## The keystone unlock
The **create→take→results→feedback loop**. It's unblocked in this order: **(1)** the Postgres migration *(built)* → **(2)** the redesigned scoring contract, a strategy that sees correct answers *(built)* → **(3)** ResultService's graded-event projection *(not built; spec 0005 serves the student read from QuizService for now, a tracked deviation)*. **Spec 0005 closed the wedge**: a seeded student takes a quiz, is graded, and sees per-question AI feedback (deterministic fallback) on a results screen. **Spec 0006 built the take-quiz UI, and [spec 0008](../docs/specs/0004-core-loop/0008-classroom-create-join/index.md) built classroom create/join**, which is what finally makes the loop walkable without the seeder: a teacher creates a class, a student joins by code, and the enrolment that gates taking (FR7) is created by a real person. **Spec 0009 then built quiz authoring, publish, and real AI generation** (tasks 1 to 6 of 7; task 1 merged, the rest on a local branch), which closes the last gap in the loop itself: a teacher goes from an empty classroom to a live, takeable quiz without the seeder. The last loop child, **teacher classroom results (UC10)**, is now **built** (spec 0010): the teacher side of the loop, a read only view of a classroom's results, aggregated on read with no new table. **The core loop (umbrella 0004) is complete.**

## Dependencies (X needs Y)
- **UC2 Create Classroom** *(built, [spec 0008](../docs/specs/0004-core-loop/0008-classroom-create-join/index.md))* — hard: identity/auth (teacher principal), Classroom model. A `Teacher`-role user creates a classroom and gets a short join code plus a shareable `/join/{code}` link; rename, archive (reversible), and code reissue included.
- **UC3 Join Classroom** *(built, spec 0008)* — hard: UC2, Enrollment model, identity. → **FR7 enforcement is now real**: any authenticated user joins by code or link, and that enrolment is the same one the take path already checked, so a student reaches and starts a quiz with no seeded row (verified end-to-end).
- **UC6 Create Quiz (manual)** *(built, [spec 0009](../docs/specs/0004-core-loop/0009-quiz-authoring/index.md))* — hard: Quiz+Question model *(built)*, UC2 (a quiz needs a classroom), identity. Create, add, edit, and remove questions of all three types, plus the **publish** step (availability window and attempt limit) that was the missing link stopping any authored quiz from reaching a student. Any attempt locks the question set.
- **UC6 AI generation (real)** *(built, spec 0009)* — hard: Claude client + **fallback** *(both built: real `claude-opus-4-8` generation, falling back to empty editable templates)*; soft: UC6 manual path *(built)*. Generation writes a **reviewed draft batch**, one per quiz, that joins the quiz only when the teacher accepts; source material can be pasted or uploaded as a PDF or docx. Spec 0009's last task (the end-to-end verify) is still open, and tasks 2 to 6 are on a local branch, not yet merged.
- **UC8 Take Quiz** — hard: migration, Quiz/Question *(built)*, UC3 enrolment (FR7), the **scoring contract redesign** *(built)*; soft: real auth (testable with a seeded identity). Take-quiz API + grading work end-to-end (spec 0005); the take-quiz **UI** is a later child.
- **UC8 AI feedback** *(built, spec 0005)* — the AI feedback strategy (`Anthropic.SDK`) runs in a background hosted service off submit, with the deterministic fallback; sends only question + correct answer + student answer per `security.md` §2. The student results screen renders it.
- **UC8 attempt rules** (`Abandoned` triggers 1–4, one-active-attempt) — hard: QuizAttempt state machine *(built)*, `Quiz.CanStart`/attempt-limit *(built)*.
- **UC9 View My Results** — hard: **ResultService** + graded-event projection + attempts existing (UC8).
- **UC10 Classroom Results** *(built, [spec 0010](../docs/specs/0004-core-loop/0010-teacher-classroom-results/index.md))* — hard: attempts existing (UC8), classroom ownership (UC2). The read side aggregates the existing attempt data on read (no separate projection, no migration): per quiz completion and average, per question difficulty, a per student roll up with a percentage normalized standing, and drill down into an attempt. Introduces the **first cross module read** (Assessment reads student names from Identity via `IUserDirectory`, a contracts only assembly), which also resolves spec 0008's deferred teacher name gap.
- **UC14 Profile** *(built)* — hard: identity; must-fix: provision the `Users` row (FK gap), partial-update, add resiliency.
- **Frontend screens** — hard: the relevant service API + the design tokens *(export landed, `ui-*.md` generated)*. soft: the YARP gateway. Same-origin in **dev** comes from the **Vite proxy**, not the gateway (foundation §7 #27, spec 0001), so the gateway is a production concern and does **not** block SPA development. The auth screens + Manage Profile also need AuthService `refresh`/`logout` *(built, PR #23)*.

## Buildable from a cold start (no prerequisites)
`global.json`; the `docker-compose` Postgres service; the Claude client wrapper **+ its deterministic fallback** (behind the strategy interface — can be built and tested before the real API key); the YARP gateway skeleton; HTTP DTO contracts; test fixtures for the state machine and scoring.

## External dependencies with lead time ⏳
- **Claude API key** — provision before the AI paths can run live (the fallback lets you build without it).
- **Railway project + managed Postgres** — provision before deploy (spec 0002 Phase 6). Docker Hub is reachable again, so local `docker compose` Postgres unblocks all dev.
- **Claude Design export** — ⏳ **blocks the UI trio** (`ui-tokens`/`ui-rules`/`ui-registry`) and therefore the SPA's final styling. Start it early; the SPA's *logic* can be built against placeholder styling.

## The one genuine tension
**Identity/auth vs. feature velocity.** Every classroom/quiz/attempt feature wants a real `Guid UserId` from a JWT, but auth is unbuilt and the code currently hardcodes IDs. Two honest paths: **(a)** build the minimal AuthService + claims plumbing first — identity is real from the start, but visible features wait; **(b)** stub a dev identity/token and build features in parallel, wiring real auth later — faster visible progress, but you re-touch every controller when real auth lands. **Recommendation:** build the *minimal* AuthService + claims extraction early (it's small and Layer-0 anyway), so identity is real everywhere from day one — just don't gold-plate it.

## Explicitly out of scope (see `foundation.md` §8 Deferred)
NotificationService; UC7/UC11/UC12/UC13; UC15–19; practice mode (UC4/UC5); question types 4–5 (multi-selection, long answer — see the UC6 fidelity note in `progress-log.md` 2026-07-24); full multiple-attempt history; save-draft; auto-save; background abandon-sweeper; event outbox. (The `quiz-trash/` cleanup that used to sit here as the final step was done 2026-07-24.)
