# 0004. The core loop (umbrella)

**Date**: 2026-07-14
**Status**: In Progress

## Summary

The core loop is the product itself: a teacher makes a classroom and a quiz, a student joins and takes it, and both sides review the result with AI written feedback. Today the plumbing exists but the loop is not usable by a human, and the AI layer that is the differentiator is entirely stubbed. This umbrella holds that loop as a set of related children built one at a time, plus the contracts they all share (how the AI is called and kept safe, how grading feeds results, how every query is scoped to the right person). The first child, the AI feedback thread, is designed and ready; the rest are named here and designed in later passes.

## Decision

Treat the core loop as an umbrella of the five children listed below, and build them one at a time rather than in a single pass. Build the wedge first: the AI feedback thread (0005), the thinnest slice that proves real Claude feedback reaching a student. The three shared contracts below (AI integration and data minimization, grading feeds results, tenancy scoping) bind every child. Each child carries its own acceptance criteria and build plan; 0005 holds AC-1 through AC-12 and is ready to build.

## Structure

Children of this umbrella. Each is a separate spec; each is designed in its own `/architect` pass and gets its number then.

- [0005 AI feedback and student results](0005-ai-feedback-and-results.md): the wedge, built as the thinnest end to end thread. Real Claude feedback on the wrong answers of a graded attempt, shown on a student results screen (supports UC8 feedback and the UC9 student read). **Built and merged.**
- [0006 Take quiz screen](0006-take-quiz-screen.md): the student take experience in the SPA, so a human drives the attempt rather than the API or the seeder (UC8 UI). Adds a list of available quizzes, a one question at a time screen with a navigator and a countdown, and answers saved to the server as they are picked. Overrides two locked foundation decisions (section 8 auto save, section 69 trigger 1); both carry follow ups to reconcile it. **Designed, ready to build.**
- (planned) Classroom create and join: teacher creates a classroom, student joins, which makes enrolment gating (FR7) real (UC2, UC3). The dependency root for real, not seeded, preconditions.
- (planned) AI quiz generation: the authoring side of the wedge, Claude assisted question generation behind the existing generation seam, with the empty editable template fallback (UC6 real).
- (planned) ResultService read side: the graded event projection into `resultdb`, the UC9 read moved off QuizService, and the teacher classroom results view (UC9 move, UC10).

## Shared contracts

The cross child rules every loop child must honor. They live here so the children stay consistent.

**1. AI integration and data minimization.** Reach the model through the Anthropic API only. Send only task necessary academic content: for generation, the teacher's topic, difficulty, and chosen materials; for feedback, `{ question, correct answer, student answer }` per question. Never send student identity, other students' data, or classroom and teacher identifiers. Every model call has a deterministic fallback, so an outage degrades the feature and never breaks the loop. The API key lives only in QuizService. Model output is untrusted: validate and cap it, and show it as plain text, never as raw HTML. Never log prompts, answers, feedback bodies, or keys; store derived output, not raw transcripts. (foundation 7 #6 and 7 #17, security.md 1, 2, 3, 6.)

**2. Grading feeds results.** QuizService is the grading authority: it grades at submit and raises `QuizAttemptGradedEvent` after the commit. The read side consumes that event. In the first child QuizService also serves the student read directly, to stay thin; when the ResultService child is built, ResultService projects from the event and takes over the reads. The graded event and the read model are the seam between grading and reporting. (foundation 7 #8, 9.)

**3. Tenancy scoping.** Every classroom, quiz, and attempt query is scoped by the authenticated `Guid UserId` from the JWT `NameIdentifier` claim, never a client supplied id. A student sees only their own work; a teacher sees only classrooms they own. Scoping is enforced in application code and must be non bypassable. An unscoped query on a tenant table is a security bug. (foundation 9, security.md 4, 7.)

## Consequences

**Positive**:
- The loop closes for one real path first (a student taking a quiz and seeing real feedback), which de risks the AI and proves the shared contracts before the loop widens.
- The differentiator stops being a stub, and the public landing claim about AI feedback becomes honest.

**Negative / tradeoffs**:
- The first child goes thin and vertical, a local exception to the breadth first stance in foundation 0. It is chosen to reduce AI risk early; the umbrella returns to breadth first for the remaining children.
- The loop is delivered in several passes, so the full teacher and student experience lands over time, not at once.

**Neutral**:
- The children can be built in more than one order; the build graph names the hard dependencies (classroom before quiz before take before results), and this umbrella starts at the wedge rather than the root by deliberate choice.

## Follow-up

- [ ] Design the next child. The natural candidates are classroom create and join (the dependency root) or the ResultService read side (which retires the temporary read location from 0005).
- [ ] Keep foundation and `build-graph.md` reconciled as children land (for example, the stale scoring is fake lines, corrected in 0005).

## Rationale

Reasoning, the slicing options, and the premise notes live in [rationale.md](rationale.md).
