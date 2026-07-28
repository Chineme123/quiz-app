# 0011. My results and progress (student) — rationale

The decision record for [index.md](index.md). `/develop` does not need this file; it is here for a human and for `/architect` on a later update.

## Context

Foundation section 7 #7 puts the full results half in v1: **UC9 (the student's own results) plus UC10 (the teacher's classroom results) plus a real results read side**. UC10 shipped as spec 0010. UC9 is only half done: spec 0005 and spec 0006 built the screen a student sees right after finishing one quiz (score, per question breakdown, AI feedback), but there is no place a student can see all their results across quizzes, and no sense of how they are doing overall. That index and that "and progress" are exactly the UC9 item foundation section 8 still lists as in scope and not built. The core loop is walkable without it (a student takes a quiz and sees the result immediately), so this is the last read surface, not a missing loop step.

Two forces shape the design. First, the pattern is already set: spec 0010 established that a classroom's results are aggregated on read from the existing attempt data with no new table and no projection (foundation section 7 #35), and foundation section 10 lists the graded event outbox and any results projection as deferred, so a stored read model would be premature here too. Second, the tenancy boundary is load bearing: a student's academic performance (scores, answers, feedback) must stay inside the classroom boundary, visible to that student and no other (security.md section 7). Unlike the teacher view, this is the student's own data, so there is no cross module name lookup and no risk of leaking another student's identity, which makes the surface smaller than 0010's.

The scope line matters because "my results and progress" could grow without bound. Foundation section 8 defers the full multiple attempt history and analytics (trends over time, every attempt listed). So the line is drawn at the latest submitted attempt per quiz and a simple per class standing, which is the useful whole a student wants (how did I do, where do I stand) without reopening the deferred analytics.

A note on filing, not a concern: the 0004 umbrella called spec 0010 "the last child," having folded the UC9 read into spec 0005. That overlooked the index and progress surface section 8 names. Spec 0011 is the genuine last read surface of the loop, and the umbrella is corrected to say so.

## Options considered

### Option 1: Aggregate on read, student scoped, reusing the spec 0010 read layer (chosen)

Serve the student's results by querying and totalling the existing attempt data live in the Assessment module, scoped to the authenticated student, reusing 0010's read repository, its latest submitted and percentage logic, and the existing per attempt results screen for the detail.

**Pros**:
- Smallest surface: one read endpoint and one list page. No table, no migration, no write path change, nothing that can go stale (every figure computed on read).
- Reuses proven code (the 0010 read repository and helpers, the existing detail screen), so it stays consistent with the module and is quick to build.
- Simpler than 0010: the student's own data means no cross module name lookup and no ownership 404 case.

**Cons**:
- Recomputes on every read. Fine at a student's scale (a handful of classes, a handful of quizzes each), but it would not serve a very large history or cross class analytics without a projection later.

### Option 2: A results projection updated on the graded event

Maintain a stored per student read model, updated when `QuizAttemptGradedEvent` fires, and read the index straight from it.

**Pros**:
- Constant time reads at any scale, and the same projection could later feed cross class or school wide analytics.

**Cons**:
- A second write path and a staleness risk (the projection can drift from the attempts if an event is missed; foundation section 9 notes the dispatch is post commit with no outbox). That cost buys nothing at a student's data scale. This is exactly 0010's runner up, deferred there for the same reason.

### Option 3: No new backend; the SPA composes the index from existing endpoints

Have the SPA gather the student's classes, quizzes, and attempts through existing calls and compute the grouping and standings client side.

**Pros**:
- No backend change at all.

**Cons**:
- There is no single endpoint today that lists which quizzes a student has finished, so the SPA would fan out many calls and still have to aggregate. It puts the tenancy scoped aggregation in the browser, the wrong layer for a security boundary, and duplicates logic that belongs in the module. It is thinner in the backend but heavier and more fragile overall.

## Rationale

Option 1 follows the forces directly. The read side pattern is already decided (foundation section 7 #35, aggregate on read, no projection), the projection path is explicitly deferred (foundation section 10), and the data is naturally small, so computing on read is the honest fit and Option 2's projection is premature. The tenancy boundary (security.md section 7) is met more simply than in 0010 because the results are the caller's own: the endpoint takes no student id, so another student's data is unreachable by construction rather than by a check, and no name crosses the module boundary. Reuse is the other driver: 0010 already built the latest submitted predicate, the percentage normalization, and the read repository, and spec 0005 already built the per question detail screen, so Option 1 is mostly composition rather than new code (the two calculation helpers first move from `ClassroomResultsAppService`'s private methods into a shared helper, a small refactor), which suits the breadth first, thinnest thread stance (foundation section 0).

The standing is per class rather than one blended number because 0010's standing is per class and averaging percents across different subjects and quiz sizes is not meaningful; grouping by class keeps each standing comparable to what the teacher sees. The latest submitted only line holds foundation section 8's deferral of full history: the student sees the score that counts, not every attempt.

On pagination: the index ships unpaginated in v1, a conscious exception to the rule that every list paginates. The honest picture, tightened after a cross check: unlike 0010's summary (bounded by one classroom's quizzes), this list grows with the student's whole enrolment history, because `Enrollment` has no term or end date and archived classes still count (AC-7), so it grows monotonically over years of use rather than being fixed to a term. It is nonetheless small for any realistic student (tens of classes at most), so v1 does not paginate; the first mitigation, if it ever matters, is capping to recent classes or terms, named in the Follow-up as expected long term growth rather than a rare edge case.

## References

**Project sources** (verifiable, in this repo):
- `context/foundation.md` section 7 #7 (UC9 and UC10 in v1), section 7 #35 (aggregate on read, spec 0010), section 8 (UC9 View My Results and Progress in scope; full multiple attempt history deferred), section 10 (results projection and outbox deferred), section 0 (breadth first, thinnest thread)
- `context/security.md` section 2 (LLM egress, not triggered here since no model call), section 7 (student academic performance stays in the classroom boundary, the tenancy boundary)
- `context/code-standards.md` (tenancy scoping by the authenticated principal; the not found over forbidden convention)
- Spec [0010 Teacher classroom results](../0010-teacher-classroom-results/index.md), the read layer, the latest submitted predicate, and the percentage normalization reused here
- Spec [0005 AI feedback and results](../0005-ai-feedback-and-results/index.md), the per attempt results screen this links into
- The code reused: `IResultsReadRepository` and `ClassroomResultsAppService` (the read plus `ReduceToLatest` and the percentage helpers), `TakeQuizFacade.GetResultAsync` and `AttemptResultDto` (the per attempt detail), `AssessmentControllerBase.GetCurrentUserId` (the principal), and `frontend/src/features/results/` (`ResultsPage`, `AttemptAnswerReview`)

**Practices & standards**:
- Compute on read for naturally bounded aggregates, rather than storing derived values that can go stale
- Deny by construction: an endpoint that takes no id for the protected resource cannot be made to return another tenant's data
- Percentage normalization before averaging scores of different sizes, so heterogeneous quizzes compare fairly
