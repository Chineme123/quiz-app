# 0010. Teacher classroom results — rationale

## Context

The core loop is otherwise complete. A teacher can author, generate, and publish a quiz (spec 0009); a student joins a class (0008), takes the quiz (0006), and sees a score with per question AI feedback (0005). The one missing piece is the teacher's side of the result: today a teacher has no way, through the product, to see how their class did. The data exists (every `QuizAttempt` carries a score and its `QuizAnswer` rows carry per question correctness), but the only reads over it are per attempt and student scoped (UC9, "View My Results"). There is no classroom wide, teacher scoped view.

This is UC10, the last named child of the core loop umbrella (0004), and it is a promise the landing page already makes to teachers ("see who is thriving and who needs a hand, at a glance"). The forces that shape the design:
- **Scale is small.** A classroom is tens of students and a handful of quizzes. The totals a teacher wants (completion, average, per question difficulty, per student standing) are cheap to compute over that much data.
- **The data is already there, and already has an owner scoping pattern.** Spec 0008 gives classroom ownership and the roster; 0005 and 0006 give the attempt and result reads. Reuse beats a parallel structure.
- **A results view is read only.** There is no new lifecycle, no write, nothing to keep consistent. The only real question is where the totalling happens.
- **Names live in another module.** The Assessment roster is a list of student ids; the display names live in the Identity module (`Profile`). A teacher facing results view needs names, not bare Guids, so this feature makes the first in process cross module read: Identity exposes a narrow, read only ids to names contract that Assessment consumes. Spec 0008 hit the same wall from the other side (showing the teacher's name on a student's class list) and deferred it; this is where that contract gets built, and it serves both directions.

## Options considered

### Option 1: Aggregate on read, no projection (chosen)

Query and total the existing `QuizAttempt` / `QuizAnswer` / `Enrollment` tables live, in the Assessment module, scoped to the owning teacher's classroom. No new table, no write path change; new read services and DTOs only.

**Pros**:
- Smallest surface: no migration, no new write path, nothing that can go stale (all figures computed on read).
- Reuses the data and the owner scoping already in the module, so it stays consistent.
- Ships fast; the whole feature is reads plus screens.

**Cons**:
- Each view runs a few grouped queries over the attempt tables. Fine at classroom scale, but not built for a very large class or cross classroom analytics.

### Option 2: Maintained results projection (denormalized read model)

Add a results table updated by a handler on the `QuizAttemptGradedEvent`, so a teacher read hits one denormalized table.

**Pros**:
- Fast reads at any scale, and a natural home for later cross classroom or school wide analytics.

**Cons**:
- A new table, a projection handler, and eventual consistency to reason about (the projection lags the grade), for a read that at this scale does not need it. It is the "compute and store a derived value before you have a measured performance problem" pattern, which goes stale and adds a second source of truth.

### Option 3: Reuse only the existing per attempt endpoint, aggregate in the browser

Have the SPA fetch each student's attempts through the existing per attempt result read and total them client side.

**Pros**:
- No new backend at all.

**Cons**:
- N network calls per classroom view, and the aggregate is computed on the client, so the class average and per question difficulty are never scoped or enforced on the server. It pushes a tenant scoped calculation to the least trusted place. Not acceptable for data this sensitive.

## Rationale

Option 1 fits the forces in Context directly. The scale is small, so the performance argument for a projection (Option 2) is a solution to a problem that has not been measured, which is the premature optimization and stale derived value trap: a projection would add a table, a handler, and eventual consistency for no gain a teacher would notice, and it would become a second source of truth that can disagree with the attempts. Computing on read keeps one source of truth (the attempts) and cannot go stale. Option 3 is genuinely the least code, but it moves a tenant scoped aggregate to the browser, where the server can no longer enforce the scoping, so it fails the security force that runs through the whole module (every figure must be computed inside the owner scoped boundary). Option 1 keeps the calculation on the server, inside the ownership check, with the least machinery.

The runner up is Option 2, and the follow up says exactly when to revisit it: if cross classroom or school wide analytics arrives, the projection earns its cost. Until then, aggregate on read is the boring, correct choice.

## References

Grounded in project sources only (no external links): the owner scoping and 404 convention (`code-standards.md` §5, `security.md` §4 and §7), the results read precedent that already serves reads directly without a projection (spec 0005), the classroom ownership and roster (spec 0008), and the list pagination convention of specs 0006 and 0008.
