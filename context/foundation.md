# Quiztin — Foundation

> **Status:** v3 — **converged**. Last updated 2026-07-08. Changes from v2: three final opens resolved — `Abandoned` triggers **1–4** (pulling a one-active-attempt rule into v1); **no slice-first** (Claude-accelerated breadth, Layer-0 must-fixes still first); **database switched to PostgreSQL** (Railway-native). No open questions remain; Phase 2 (the rest of `context/`) may proceed.
> **Source of truth.** Every other file in `context/` references this one; none restate it. If any file disagrees with this one, this one wins.
> **Name note.** The **product/brand name is "Quiztin"** (locked). Existing **code identifiers** — `QuizApp.sln`, `QuizService`, `QuizAppService`, `QuizDbContext`, etc. — keep their current names for now; renaming them to match the brand is an optional, low-priority refactor, not v1 work.

**Status key:** ✅ locked/built · 🟡 in progress · ⬜ planned · 🕗 TBD (decide later) · **[LOCKED]** settled decision · ⚠️ known drift/contradiction to fix.

---

## §0 Build constraints

The forcing function. Scope discipline follows from these.

- **Solo developer.** One person (GitHub `Chineme123`, Covenant University). The two git identities in the history are the **same person on two machines/surfaces**, not a team. → **Solo project: personal `CLAUDE.md`, no team `COLLAB.md` layer.** [LOCKED]
- **Capacity: ~10 hours/week, no hard deadline.** [LOCKED] Completely open timeline. Development is **Claude-accelerated**, so raw build time is not the binding constraint the hours alone would suggest.
- **Academic context, product ambition.** Built as coursework under the **Agile Unified Methodology (AUM)** — design-first, use-case-by-use-case, professor-assigned GRASP/GoF patterns — but the intent is a **real, demoable product** (§3). [LOCKED]
- **Dev environment.** Local on the developer's **macOS** machine: **.NET 10 SDK + `dotnet-ef`** (installed under `~/.dotnet`) and **PostgreSQL 17 via Homebrew** on `localhost:5432` — Docker Hub pulls are blocked on the developer's network, so containers aren't run locally. The prior **GitHub Codespace / devcontainer** path was **removed (2026-07-10)** and is no longer used.

**Implication (the discipline this forces):** the developer builds **breadth-first with Claude, not a thin vertical slice** [LOCKED this session] — refactoring is cheap enough that integration risk is managed as it arises rather than de-risked up front. Two things hold regardless of that choice: (1) the **Layer-0 must-fixes** (§8) — regenerate the migration, fix the scoring contract, pin the framework, fix identity/auth — are genuine prerequisites; every feature sits on them, so they come first. (2) `build-graph.md` states **hard dependencies** (what cannot be built before what), not a prescribed order — the developer picks the path through it.

---

## §1 What it is

**One-liner:** Quiztin is a classroom quiz web app where **teachers author and publish quizzes inside classrooms**, **enrolled students take them and are scored immediately**, and both sides **review question-by-question results with AI-generated feedback**.

The core loop is **create quiz → take quiz → see results → review feedback**. The wedge is the **AI layer**: AI-assisted question generation at authoring time, and AI per-question feedback at review time. That layer is the product's differentiator — today it is entirely stubbed (§10, §11), which is why it is explicitly **in scope for v1** (§7 #6), backed by a deterministic fallback so the loop never depends on the model being up.

**Brand voice** (decided 2026-07-09, drives the UI): **friendly & approachable** — a helpful, encouraging companion, not a bureaucratic testing tool. Warm and personable (the name "Quiztin" reads as a friendly character), but credible for teachers. Non-negotiables the UI must serve: the take-quiz experience stays **calm and low-anxiety**, the app is **accessible**, and AI feedback reads as **supportive**, not a grade stamp. (This is the seed for `ui-rules.md` §0; detail in `claude-design-handoff.md`.)

---

## §2 Who it's for

- **Teachers** — own classrooms, author/publish quizzes, view classroom-wide results. Academic profile: School, Department, Instructor Type (Professor / Assistant Professor / TA / High School Teacher).
- **Students** — enrol in classrooms, take available quizzes, review their own results + feedback. Academic profile: School, Department, Academic Level (Freshman → Graduate).
- A generic authenticated **User** underlies both; **role** (from the auth token) drives differentiation. **Admin** (UC16/UC17) is designed but out of v1.
- **Setting:** academic classroom, one instructor per classroom, roster-gated access (FR7: only enrolled students may take a classroom's quizzes — enforceable in v1, UC2/UC3 in scope).

---

## §3 Success & stage

- **Framing (governing):** **Real product, built via coursework.** [LOCKED] "Done" means the **core loop ships and demos end-to-end**, not merely that a PDF is accepted. The AUM design docs become the *tracked design ledger* (§7 #19), not the deliverable.
- **Stage:** early-foundation brownfield. Active development is a ~13-day sprint (2026-02-04 → 2026-02-17) after a long gap. Built so far: **UC6** (create quiz), **UC8** (take quiz), **UC14** (create profile). Two of five services are real (Quiz, User); three are scaffolds (§8).
- **Success criteria (v1):** a student can log in, join/be-enrolled in a classroom, take a published quiz there, get a real score, and review per-question AI feedback; a teacher can create a classroom, author (optionally AI-assisted) and publish a quiz, and see classroom results.

---

## §4 Guiding principles

Each settles arguments later; the *why* for specific choices lives in §7.

1. **Design-first, one use case at a time (AUM).** Each UC gets a design pass (business description → domain model → GRASP/GoF → UML/sequence → UI brief) before code.
2. **Patterns are applied deliberately, and must be real in code.** GoF/GRASP patterns are a requirement, not decoration — a pattern that adds no behavior (the current Command "pattern," §7 #11) is drift to fix, not a badge to keep.
3. **Clean/Onion architecture per service.** Domain (zero deps) ← Application ← Infrastructure ← API; dependencies point inward, interfaces in the core.
4. **Rich, encapsulated domain entities.** Private setters, private EF constructors, guard clauses, behavior methods (QuizAttempt and Profile already do this). Anemic entities with public setters (current Quiz) are drift to tighten.
5. **The context system is the durable brain.** The design half of a design-first project must live in git, not on one laptop (§11). `foundation.md` is the source of truth; the corpus is committed alongside it.
6. **Close the loop before widening it.** A working create→take→results→feedback loop for one classroom beats broad coverage of half-built use cases.
7. **Explicit over magic**, with visible failure modes — name the seams where the code leans on framework magic (DI-resolved observers, post-commit dispatch without an outbox — §9).

---

## §5 Core model

The central objects and their lifecycles. Detail (relationships, cardinalities) lives in `architecture.md`; the decisions live here.

- **QuizAttempt — the flagship aggregate.** Lifecycle is a real **State machine**: `NotStarted → InProgress → Submitted → Graded → Reviewable`, plus terminal `Abandoned`. Illegal transitions throw; each concrete state permits only its legal move. The state object is `[NotMapped]`, rehydrated at load from a persisted `CurrentStateName` string. **`Abandoned` is reached by triggers 1–4** [LOCKED]: (1) time-limit expiry, (2) availability-window close — both evaluated **lazily on access**; (3) explicit student "quit" — an eager state action; (4) **superseded** — starting a new attempt abandons a prior unfinished one, which brings a *one-active-attempt* rule + configurable attempt limit into v1 (full multiple-attempt history stays deferred, §8). No background sweeper needed for v1. Encapsulation leaks to tighten: `TotalScore` and `RowVersion` are publicly settable.
- **Quiz — aggregate root.** Owns its `Questions` (cascade delete), belongs to a Classroom, enforces availability/attempt rules via `Quiz.CanStart`. Currently anemic (public setters) — tighten per principle 4.
- **Question — TPH hierarchy.** Single `Questions` table, string discriminator; abstract `Question` + `MultipleChoiceQuestion` / `TrueFalseQuestion` / `ShortAnswerQuestion`. `Options` persisted as `jsonb` (Postgres) with an explicit `ValueComparer`. ⚠️ Design specifies **5** types (adds Multi-select, Long Answer) + a "composition must sum to 100%" rule; code implements **3** — the extra two are deferred (§8).
- **Classroom → Enrollment → Quiz.** Classroom owned by a teacher (privacy: open vs invite-only); Enrollment is the student roster with a unique `(StudentId, ClassroomId)`. **FR7 is enforceable in v1** because UC2 (create) + UC3 (join) are in scope (§7 #9, §8).
- **User ↔ Profile.** One-to-one, shared PK/FK on `UserId`, cascade delete. ⚠️ UserService never creates the `Users` row a Profile FK requires (§10) — fix in v1.
- **Identity (canonical):** every user is a **`Guid UserId`, always read from the JWT `NameIdentifier` claim.** [LOCKED — §7 #13].
- **Supporting:** `ProcessedCommand` (submission idempotency), domain events (`QuizAttemptGradedEvent`).

---

## §6 Core flows & surfaces

**The loop (v1):**
1. **Create classroom (UC2, QuizService):** teacher creates a classroom (open or invite-only).
2. **Join classroom (UC3):** student enrols (making FR7 real).
3. **Create quiz (UC6, QuizService):** teacher configures a quiz, authors questions manually **or via AI generation** (Claude, with fallback), reviews, publishes.
4. **Take quiz (UC8, QuizService):** enrolled student starts an attempt (`TakeQuizFacade`: enrolment check → availability/attempt-limit → idempotent command execution → **real scoring** → **AI feedback (with fallback)** → transactional save → post-commit graded event).
5. **See results (UC9, ResultService):** student views their score + per-question breakdown + AI feedback.
6. **Classroom results (UC10, ResultService):** teacher views classroom-wide results.

**Surfaces:**
- **Frontend:** a **React + Vite SPA** [LOCKED — §7 #5] in `frontend/`, reaching the backend through **one API gateway origin** (§7 #15), not five service ports. Figma/Claude-Design UI briefs already exist per UC.
- **Backend:** five .NET microservices behind the gateway; Swagger per service for dev.

---

## §7 Locked decisions

The heart of the file. Numbered so other files cite `foundation.md §7 #N`. Rows marked *(this session)* were decided in the foundation conversation; the rest are pre-existing decisions embedded in the code, now made explicit.

| # | Decision | Reasoning | Rejected alternative |
|---|----------|-----------|----------------------|
| 1 | **Monorepo: `frontend/` + `src/Services/` + `docs/` + `quiz-trash/`, one repo** *(this session)* | One place for the whole product; frontend and backend version together | Split repos (more overhead for a solo dev) |
| 2 | **Move legacy/scaffold into `quiz-trash/`** (deferred cleanup) *(this session)* | Keep the tree clean without deleting history | Delete outright (loses reference) |
| 3 | **Real product built via coursework** framing *(this session)* | The demoable loop is the bar; AUM docs are the design ledger | Coursework-only; portfolio-only |
| 4 | **Product name = "Quiztin"**; code identifiers keep current names for now *(this session)* | Brand locked; renaming code is an optional refactor, not v1 work | Keep "QuizApp"; rename all code now (churn for no v1 value) |
| 5 | **React + Vite SPA** for the frontend *(this session)* | Cleanest pairing with a REST/.NET API; largest ecosystem; matches the Figma/Claude-Design handoff | Blazor (worse design-handoff fit); Next.js (no SSR/SEO need) |
| 6 | **AI in v1 (Anthropic Claude): both question-generation AND per-question feedback, each with a deterministic fallback** *(this session)* | The AI layer *is* the wedge, so ship it — but the loop must never *depend* on the model: a failed call falls back (empty editable template for generation; rule-based/"feedback unavailable" for feedback) | Deferring AI to vNext (ships no wedge); AI with no fallback (a model outage breaks the loop) |
| 7 | **Full results half in v1: UC9 + UC10 + a real ResultService** *(this session)* | Without a results surface the loop dead-ends at submission | Minimal UC9-in-QuizService only; leaving the loop open |
| 8 | **ResultService = read/reporting; QuizService = grading authority** *(this session)* | QuizService holds questions + answers + the attempt lifecycle, so it grades; ResultService owns UC9/UC10 (later UC11/UC13) as reads/aggregation, fed by the graded-event projection (`DashboardProjectionUpdater` + `QuizAttemptGradedEvent`) | ResultService owns evaluation (splits the lifecycle); keep it all in QuizService (fails the "own service" goal) |
| 9 | **Classrooms in v1: UC2 Create + UC3 Join, making FR7 real** *(this session)* | Enrolment-gated taking is a core rule; faking a default classroom leaves FR7 unenforceable | Seed a default classroom only (FR7 stays fake); defer classrooms |
| 10 | **Database: PostgreSQL** (Railway-native), DB-per-service on one shared Postgres instance; **Npgsql** EF provider *(this session)* | Railway has no managed SQL Server (self-hosting the `mssql` container is heavy: RAM + licensing); Postgres is managed, lighter, one-click, and free-tier friendly | SQL Server 2022 (the current code target — heavy/awkward on Railway) |
| 11 | **Five .NET microservices** (Auth, User, Quiz, Result, Notification), **Clean Architecture** per service | Established in the code + scaffold; isolates domains | Monolith |
| 12 | **GoF/GRASP patterns per UC**, real in code: State, Strategy, Factory, Observer, Facade | Course requirement + genuinely fit the domain | ⚠️ The **Command** "pattern" is a hollow passthrough — give it real behavior or drop it |
| 13 | **Target framework: .NET 10, pinned with `global.json`; EF Core 10 (Npgsql provider)** *(this session)* [LOCKED] | Devcontainer/tooling/build output already on 10, plus an explicit "upgrade to .NET 10" commit; align *up* and pin it | Freeze at net8; leave unpinned (build drift) |
| 14 | **Canonical identity: `Guid UserId` from the JWT `NameIdentifier` claim**, everywhere; no hardcoded IDs *(this session)* | One identity source for auth + tenancy + FKs | Per-service ad-hoc IDs (the current broken state) |
| 15 | **Minimal AuthService issues HS256 JWTs**; single shared issuer + audience across services; signing secret in config/user-secrets/env, never committed *(this session)* | Quiz + User validate tokens no one issues; auth is dead until something mints them | Third-party IdP (overkill); keep hardcoded IDs (insecure) |
| 16 | **One API gateway (YARP reverse proxy) as the single frontend origin; CORS configured there** *(this session)* | A browser SPA can't sanely call 5 shifting-port origins with no CORS; a gateway hides the sprawl and centralizes CORS/auth | SPA calls services directly (CORS × 5, port chaos) |
| 17 | **`security.md` split out as its own authority file, encoding data minimization** *(this session)*: model access is **Anthropic API only**; **send only task-necessary academic content** (generation: teacher's topic/difficulty/materials; feedback: `{question, correct answer, student's answer}` per question); **never send student identity** (name/email/UserId) or other students' data; store derived feedback, not raw model transcripts; never log prompts/answers/keys | Student academic data + third-party model egress + already-committed secrets need a real data-handling authority; minimization keeps student PII off the wire by construction | Send full attempt + student identity; keep security as weak sections in `code-standards.md` |
| 18 | **EF Core conventions:** TPH question inheritance, `Options` as `jsonb` + `ValueComparer`, optimistic concurrency via Postgres **`xmin`** system column (Npgsql), **connection resiliency (`EnableRetryOnFailure`) on every service** | Matches QuizService's proven shape, adapted to Npgsql; UserService currently lacks resiliency and must gain it | SQL Server `rowversion` (provider-specific, now gone); no concurrency token; no retry |
| 19 | **Idempotent submission:** `ProcessedCommand` keyed on a client `CommandId`, checked+marked inside an execution-strategy-wrapped transaction | Already built and solid; safe under connection retries | Non-idempotent submit (double-grade risk under retry) |
| 20 | **Keep the design corpus in `docs/` (converted to markdown) and the context system in `context/`; adopt QuizService's conventions as canonical across services** *(this session; corpus converted + moved 2026-07-09)* | The design-first brain must be readable and in the repo tree (§11); the two real services drifted — pick one convention | Leave the corpus in binary Word files (bus factor 1); let conventions diverge |
| 21 | **No FluentValidation / AutoMapper — manual validation + manual DTO mapping** | Deliberate: fewer deps, explicit control; already the house style | Adding those libs (unneeded weight) |
| 22 | **Hosting: containerized (Docker) → Railway**, with **Railway-managed Postgres** *(this session)* | Simplest path for a solo dev on a student budget; Docker keeps it portable; Postgres is Railway-native | Azure (heavier); Fly/Render (Railway chosen); self-hosted SQL Server container (heavy) |
| 23 | **Frontend language: TypeScript (strict)** *(spec 0001)* | The API boundary is where drift bites; types plus Zod catch it before a screen does | Plain JavaScript (no boundary safety) |
| 24 | **Tailwind v4, bound to the design tokens by reference (`@theme inline`), Preflight not loaded** *(spec 0001)* | Makes `bg-primary` compile to `var(--primary)`, so the "semantic tokens only, no raw hex" rule is enforced by the compiler rather than by review; `design-system/tokens/base.css` is already the reset, so a second one would fight it | CSS Modules (rule survives on discipline alone); Tailwind with Preflight (two competing resets) |
| 25 | **React Router v7 data router (`createBrowserRouter`), loaders and actions unused; TanStack Query owns all server state** *(spec 0001)* | Nested routes and error boundaries without framework mode, whose Vite plugin and server rendering are the same machinery rejected in #5; one data layer, not two | Framework mode (adds a Node runtime to a .NET architecture); loaders plus Query (two caches, one truth) |
| 26 | **Session: short-lived access token held in memory, rotating refresh token in an `HttpOnly` cookie; AuthService gains `POST /api/auth/refresh` + `POST /api/auth/logout`, with reuse detection and a grace window** *(spec 0001)* | A token in `localStorage` is readable by any injected script, and an 8h token guarding student records is an unacceptable blast radius (`security.md`); the cookie is unreachable by script and rotation bounds replay | Token in `localStorage` (script-readable); server-held session (a second runtime to operate) |
| 27 | **The frontend is same-origin with the API: Vite dev proxy now, the YARP gateway (#16) later. No CORS in any service** *(spec 0001)* | CORS in five services is five places to get it wrong, then a sixth at the gateway; same-origin means the app never learns the concept, and the gateway move is config, not a rewrite | CORS per service (port chaos, five configs) |
| 28 | **Build only against endpoints that exist; no mock layer** *(spec 0001)* | A mock is a second source of truth that agrees with the backend right up until it doesn't, and the divergence surfaces late; scope follows the real API surface instead | Mocked endpoints to unblock screens (Facade-first, rejected against §0) |

**Approved-dependency detail and per-library gotchas live in `library-docs.md`; the numbered reasoning lives here.**

---

## §8 Scope

### In (v1)
- **UC2** Create Classroom + **UC3** Join Classroom — makes FR7 real. ⬜ (UC2 design doc is a mis-titled UC6 copy — rewrite needed.)
- **UC6** Create Classroom Quiz — teacher authoring + publish, **with real AI generation** (Claude + fallback). ✅ built (manual); 🟡 AI real.
- **UC8** Take Classroom Quiz — attempt lifecycle + submission + idempotency, **with real scoring + real AI feedback** (Claude + fallback), **+ configurable attempt limit & one-active-attempt rule** (`Abandoned` trigger 4). ✅ built (flow); 🟡 scoring/feedback/attempt-rule real.
- **UC9** View My Results & Progress (student) — ResultService read. ⬜
- **UC10** View Classroom Results (teacher) — ResultService read. ⬜
- **UC14** Create/Update User Profile — role-aware. ✅ built (needs `Users`-row + partial-update fixes, §10).
- **Minimal AuthService** issuing JWTs; end-to-end auth + `[Authorize]` enforcement. ⬜
- **API gateway (YARP)** + CORS. ⬜
- **React + Vite SPA** covering the loop screens. ⬜

### Must-fix before/inside v1 (repairs the loop depends on — not new features; these are Layer 0)
- ⚠️ **Migrate to Postgres + regenerate the EF migration** — drop the SQL Server migration, add Npgsql, generate a fresh `InitialCreate` against Postgres including *all* current entities (QuizAttempt/QuizAnswer/Enrollment/ProcessedCommand were missing). One action covers the provider switch and the stale-migration bug. **Step one.**
- ⚠️ **Redesign the scoring contract** so a strategy can see the correct answers (current `IScoringStrategy.Score(QuizAttempt)` structurally can't grade).
- ⚠️ **Pin the framework** (`global.json`) and reconcile every `.csproj` to .NET 10 (§7 #13).
- ⚠️ **Fix identity + auth** to §7 #14/#15 (remove hardcoded IDs, unify JWT audience, uncomment `[Authorize]`).

### Out / cut (→ `quiz-trash/` in the final cleanup pass)
- WeatherForecast scaffolding (QuizService.API + all three stub services).
- Empty `UnitTest1.cs` placeholders.
- The three empty stub services **as scaffolds** — Auth/Result get *rebuilt* for v1; Notification stays out (below).

### Deferred (designed, later phase)
- **`quiz-trash/` cleanup pass itself** — [LOCKED as the final step].
- **NotificationService** — no v1 loop step needs it.
- UC7 Assign Quiz; UC11 Quiz Metrics; UC12 Configure Grading Style; UC13 Export Results.
- UC15 View/Update Profile (full); UC16 Account Status/Admin; UC17 Roles & Permissions; UC18 Notification Preferences; UC19 Activity Summary.
- **UC4/UC5 Practice mode** — explicit nice-to-have (Phase 4).
- 4th & 5th question types (Multi-select, Long Answer) + the 100%-composition rule.
- **Full multiple-attempt history/analytics** (v1 has only the one-active-attempt rule + attempt limit); Save-Draft; in-progress auto-save; a background `Abandoned` sweeper; domain-event outbox.

---

## §9 Architecture keystones

- **Keystone unlock:** the **create→take→results→feedback loop**. Today it dead-ends at submission because (a) results surfaces (UC9/UC10, ResultService) are unbuilt and (b) there's no valid migration for the attempt tables. The Postgres migration + a real ResultService read-side are the unlocks. (Detail → `architecture.md`.)
- **Tenancy / isolation:** DB-per-service on one shared Postgres instance; classroom/teacher isolation is enforced only in application code (ownership checks), not at the data layer. v1 keeps app-layer scoping but must make it **consistent and non-bypassable** (every query scoped by the authenticated principal, never a client-supplied ID).
- **Frontend → backend:** one **YARP gateway** origin (§7 #16); the SPA never sees the 5-service sprawl or shifting ports.
- **Grading vs reporting boundary:** QuizService grades at submission; ResultService projects from `QuizAttemptGradedEvent` and serves reads (§7 #8). The projection path (`DashboardProjectionUpdater`) is currently a no-op to implement.
- **Explicit-over-magic seams to watch:** the Observer resolves handlers via DI rather than classic subject-attach; graded events dispatch **post-commit with no outbox** (acknowledged in code comments) — a real at-least-once gap, accepted for v1, flagged in §10.

---

## §10 Known scale seams

Honest "not scaling yet, and here's what replaces it":

- **AI is stubbed today** — `StubLLMQuestionGenerationStrategy` returns canned placeholders; v1 replaces it with a real Claude call behind the existing strategy seams, **with a deterministic fallback** (§7 #6) so an outage degrades gracefully.
- **Scoring is fake** — flat 10 pts for any non-empty answer, can't see correct answers; replaced by the redesigned scoring contract (§8 must-fix).
- **Provider migration cost** — moving SQL Server → Postgres touches the migration (regenerated fresh), the `Options` mapping (`nvarchar`+JSON → `jsonb`), the concurrency token (`rowversion` → `xmin`), connection strings, and `docker-compose` (swap the `mssql` image for `postgres`). One-time v1 cost.
- **Graded-event dispatch is post-commit, no outbox** — can silently drop events on failure; acceptable for v1, replaced by an outbox when reliability matters.
- **Committed secrets** — plaintext DB password + JWT signing key committed in code/appsettings → all move to secrets (§7 #15/#17, `security.md`).
- **UserService FK gap** — Profile insert needs a `Users` row UserService never creates; first-time profile creation throws until a user is provisioned. Fix in v1.
- **Profile PUT is full-replace** — nulls overwrite Bio/Avatar/School/Department; make partial-update-safe.
- **UserService lacks connection resiliency** (QuizService has it) — add per §7 #18.

---

## §11 The deepest risk

**The design-first brain lives on one machine — bus factor of one.** As of 2026-07-09 the design corpus has been **converted to markdown and consolidated in `docs/`** (originals archived in `quiz-trash/`), and this context system is in `context/` — so the AUM rationale, pattern choices, FR mappings, and UC catalog are now readable and in the repo tree rather than trapped in binary Word files. But the working tree is still **local-only and not pushed to any remote**, by the developer's deliberate choice — so the bus-factor risk (lose the machine, lose the project) is an **accepted tradeoff**, not an oversight. Re-evaluate if/when that changes.

**Runner-up (now mitigated by design):** the product's wedge is AI, and AI is 100% stubbed. v1 makes it real (§7 #6) — and because a **deterministic fallback** is locked, a Claude outage degrades the experience instead of breaking the loop. The "AI" promise never blocks a working demo.

---

## §12 Open questions

**None — converged (v3).** All Phase-1 questions are resolved (framing, frontend, AI scope + provider + fallback, results topology, classrooms, framework, identity, auth, gateway, security stance, hosting, database, name, capacity, `Abandoned` triggers, sequencing). Phase 2 builds the rest of `context/` in dependency order: `README.md` → `project-overview.md` → `architecture.md` → `security.md` → `code-standards.md` → `library-docs.md` → `build-graph.md` → `progress-log.md`, with the UI trio PENDING until the Claude Design export exists. New decisions made during the build are recorded in `progress-log.md` and rippled back here per the drift rule.
