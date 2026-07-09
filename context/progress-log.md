# Quiztin — Progress Log

> Living build record. Newest first — prepend. Current settled state lives in `foundation.md`, not here.

## Standing instruction for the AI agent (do this every time)

After completing any work in this project, before ending your response, add a progress entry at the TOP of the Entries section. This is mandatory, the same way reading the context files first is mandatory. If a single prompt produced several distinct pieces of work, put them in one entry as clearly itemized parts. If you made a `decision` that changes anything in `foundation.md` or another context file, **update that file too** and add a `docs` entry noting the update — context files must never drift from what was decided.

## Entry template

Category one of: `feature` · `fix` · `refactor` · `chore` · `decision` · `docs`

```
### [category] Short title
- **Date:** YYYY-MM-DD
- **Area:** {backend / apps/frontend / gateway / db / infra / context / docs}
- **What:** one line; itemized parts below if several
- **Notes:** gotchas, limitations, follow-ups
```

## Entries

### [chore] .NET 10 toolchain installed locally; Layer-0 framework pin done — solution builds + tests GREEN
- **Date:** 2026-07-09
- **Area:** infra / backend
- **What:** (1) Installed the .NET 10 SDK (10.0.301) + `dotnet-ef` (10.0.2) to `~/.dotnet` on the local Mac (no admin), persisted PATH/DOTNET_ROOT to `~/.zshrc` — build/test/migrate now run fully locally (user chose to stay off a remote). (2) **Framework pin (Layer-0 must-fix):** all 22 `.csproj` → `net10.0`; all `Version="8.0.0"` EF/JwtBearer packages → `10.0.0`; rewrote the malformed `QuizService.API.csproj` (duplicated headers → MSB4025); added root `global.json`. (3) **Fixed 3 pre-existing bugs the build then surfaced, properly (per code-standards §3, user chose "fix properly"):** abstracted the transaction out of `QuizService.Domain` (`IQuizAttemptRepository` now exposes `ExecuteInTransactionAsync(Func<Task>)` instead of EF `IExecutionStrategy`/`IDbContextTransaction`; execution-strategy+commit/rollback moved into `QuizAttemptRepository`; `TakeQuizFacade` simplified accordingly); implemented the missing `QuizRepository.IsStudentEnrolledAsync` (the FR7 enrolment check) + aligned `IQuizRepository` nullable returns; added missing `using`s to `QuizService.API/Program.cs`. (4) Added `QuizService.Tests` to `QuizApp.sln` (it existed but was never in the solution).
- **Result:** `dotnet build QuizApp.sln` → **0 errors**; `dotnet test` → **13 passed / 0 failed** (QuizService.Tests 4, UserService.UnitTests 9).
- **Notes:** Kept EF Core **SqlServer** provider for now — the **Postgres swap is the next Layer-0 step** (then: regenerate migration on Postgres, redesign scoring contract, fix identity/auth). A few nullability warnings remain in `IQuizAttemptRepository` (non-blocking; sweep during a later cleanup).

### [decision] Brand voice: friendly & approachable
- **Date:** 2026-07-09
- **Area:** context / docs
- **What:** Chose the design/brand direction for the Claude Design intake: **friendly & approachable** (helpful companion, warm/personable, but credible for teachers). Recorded in `foundation.md` §1 (brand voice) and `claude-design-handoff.md`; it seeds `ui-rules.md` §0 when the UI trio is generated.
- **Notes:** Non-negotiables regardless of styling: calm/low-anxiety take-quiz, accessibility, supportive AI-feedback tone. Palette/radius/type deliberately left for Claude Design to propose within this direction (no invented palette).

### [chore] Corpus converted to markdown; quiz-trash cleanup started
- **Date:** 2026-07-09
- **Area:** docs / infra / context
- **What:** (1) Converted all 10 valid corpus `.docx` files to markdown in `docs/` (faithful deterministic extraction, incl. scenario tables), added `docs/README.md` index + a `docs/uc02-create-classroom.md` placeholder (original was a broken UC6 copy); copied the two `.drawio` diagrams to `docs/diagrams/` and moved `context-system.skill` to `docs/`. (2) Started the `quiz-trash/` cleanup: moved WeatherForecast scaffolds (10 files), empty `UnitTest1.cs` (2), `TROUBLESHOOTING.md`, and the original `.docx` corpus folder into `quiz-trash/`; deleted `.DS_Store`. (3) Repointed references in `foundation.md` §11/§7 #20, `architecture.md`, `claude-design-handoff.md`, `ui-rules.md`.
- **Notes:** No product code changed; WeatherForecast removal is safe (no real references). `TROUBLESHOOTING.md` trashed per user; its still-useful Docker/EF/gh cheat-sheet survives in `docs/project-environment-and-architecture.md`. Cleanup started early (foundation §8 lists it as the final step) at user request — the stub *service* folders (Auth/Result/Notification) were left in place since Auth/Result get rebuilt and moving whole projects would break `QuizApp.sln`.

### [docs] Context system established
- **Date:** 2026-07-08
- **Area:** context
- **What:** Ran the context-system skill in brownfield mode. Built `foundation.md` (v3, converged) by mining the code + the AUM design corpus + git history, then drafted the rest of the system: root `README.md` + `CLAUDE.md`, and `context/{project-overview, architecture, security, code-standards, library-docs, build-graph, progress-log}.md`. UI trio created as PENDING stubs.
- **Notes:** No code changed yet — this is the design/decision layer. Key decisions locked this session: monorepo; React+Vite SPA; AI (Claude) in v1 with deterministic fallback; full results half (ResultService as read-side, QuizService grades); UC2/UC3 in v1 (FR7 real); **switch SQL Server → PostgreSQL**; .NET 10 + EF10 pinned; Guid identity from JWT; YARP gateway; security.md split out. Layer-0 must-fixes identified: regenerate migration on Postgres, redesign scoring contract, pin framework, fix identity/auth. Corpus/skill remain untracked by the developer's deliberate local-only choice. `quiz-trash/` cleanup is the final step.

### [context] Pre-context-system history (reconstructed from git)
- **Date:** 2026-02 (see `git log` for exact dates; active sprint ~2026-02-04 → 2026-02-17)
- **Area:** backend / db / infra
- **What:** The build state this context system was created on top of, newest-first:
  - `feat: UC14 — User Profile Implementation` (UserService: role-aware profile, Strategy pattern). *(cebd5d1)*
  - `feat: UC8 — Take Classroom Quiz Implementation` (QuizService: QuizAttempt state machine, TakeQuizFacade, scoring/feedback strategies stubbed, idempotent submission). *(1e56af3)*
  - `docs: troubleshooting` added/updated → `TROUBLESHOOTING.md`. *(a6aaa9f, 15d8e1d)*
  - `refactor(quizservice): EF Core ValueComparer + connection resiliency`. *(2fe9cb9)*
  - `chore: apply .gitignore; untrack build/AI artifacts`. *(5dccd68)*
  - `feat: setup remote workspace + upgrade to .NET 10.0`. *(02eaee2)*
  - `Initial commit: UC6 quiz creation with dev seed support` (QuizService: Composite/Factory/Strategy, DataSeeder). *(01ae1f6)*
- **Notes:** Reconstructed from commit messages, not authored contemporaneously. Known drift carried in from this history is catalogued in `foundation.md` §8 (must-fixes) and §10 (scale seams): stale/SQL-Server migration, fake scoring, hardcoded JWT + IDs, WeatherForecast scaffolds, UserService FK gap, three stub services.
