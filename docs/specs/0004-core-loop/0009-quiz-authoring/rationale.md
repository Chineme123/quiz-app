# 0009. Quiz authoring, AI generation, and publish — rationale

The build spec is in [index.md](index.md). This file is the decision record: the problem, the options weighed, and why. `/develop` does not need it.

## Context

> ⚠️ Premise note: this spec spans five independently implementable decisions (publish, real AI generation, server saved drafts, file backed source material, and the full authoring UI), where the architect process would normally narrow to one. The concern is real and was raised twice: publish alone closes the loop (a teacher can already create a quiz and add questions by API, and the only missing link is that nothing sets `IsPublished`), while file upload and the authoring UI each carry their own security and storage surface and would each carry a large build. Spec 0008 was a single decision and consumed a full working session; this is five, two of them heavier than anything in 0008. The engineer chose to keep all five in one spec after the split was offered. That is a valid override; it is recorded here so the size is a known, deliberate tradeoff rather than a drift.

The core loop (spec 0004) is create a quiz, take it, see results, review AI feedback. Spec 0005 made feedback real, spec 0006 built the take screen, and spec 0008 built classroom create and join so a real student can enrol. The authoring side is the last gap. Today a teacher can create a quiz and add questions through the API, but no endpoint sets `IsPublished`, so `Quiz.CanStart` always refuses (it checks `IsPublished` first) and the quiz never reaches a student. The only publishable quiz is the one the dev seeder marks published. So the loop is walkable only on seeded content.

The generation seam exists but is a local stub. `IQuestionGenerationStrategy` and `StubLLMQuestionGenerationStrategy` synthesise canned questions with no network call, while spec 0005 already built a real Claude pipeline next to it (the community `Anthropic.SDK`, a flag plus key gate, a deterministic fallback, output validation, a background worker). So the authoring side has a working template to copy from on the same stack.

`security.md` already legislates this feature ahead of time. Section 1 marks generated questions as untrusted output that must be validated against the question schema before persistence, and requires a deterministic fallback on every model call. Section 2 says a generation call may send the teacher's topic, difficulty, and chosen source materials, and must never send student data or classroom and teacher identifiers. Section 3 keeps the key server side. Section 6 forbids logging prompts or responses. The design honours these rather than discovering them.

Two forces make the shape non obvious. First, the project stays on the community `Anthropic.SDK` (the engineer's choice), which does not constrain output to a JSON schema, so the model's questions must be validated by hand, and the existing `QuestionFactory` throws on a bad question, with no partial success handling in `GenerateQuestionsAsync` today, meaning one malformed question currently fails the whole batch. Second, easier AI driven editing raises a grading integrity question the loop has so far dodged: what happens to a quiz's questions once students have attempted it. The feedback pipeline reads questions fresh at grading time, so an edit after attempts can change what a student is graded against.

## Options considered

### Option 1: all five parts as one child spec (chosen)

Publish, manual authoring, real generation, server saved review drafts, and file backed source material, plus the authoring UI, as a single child of the 0004 umbrella.

**Pros**:
- Delivers the whole authoring experience in one coherent decision record, so the teacher side lands as a unit rather than in fragments the engineer has to reassemble.
- Matches the engineer's stated intent to keep AI generation, upload, drafts, and the editor together.

**Cons**:
- Spans five independently buildable decisions; the largest child by a wide margin, with two new security surfaces and a large UI, and will build over several sessions.
- Harder to review as one artifact; a defect in any one part blocks a spec that is otherwise ready.

### Option 2: split at the heavy parts

0009 = publish plus real generation from a topic with a client held review draft and the authoring UI; a later 0010 = source material upload and parsing plus server persisted drafts.

**Pros**:
- Each spec is roughly 0008 sized and reviewable on its own; the two subsystems that carry their own security and storage surface (file parsing, persisted drafts) get their own review.
- Still ships a loop a real teacher can fully drive (create, generate from a topic, publish), just without file upload.

**Cons**:
- Two specs and two build arcs instead of one; the teacher experience arrives in two waves.
- Splits generation from its source material and drafts, which the engineer wanted together.

### Option 3: publish only, generation next

0009 = publish plus a minimal manual editor; all AI generation, upload, and drafts become a later spec.

**Pros**:
- Smallest possible slice; closes the loop fastest and could ship in one short session.

**Cons**:
- Leaves the AI differentiator unbuilt on the authoring side for another whole spec, when the seam and the feedback template already exist.
- A manual only editor is a thin teacher experience to ship as the headline authoring slice.

## Rationale

Option 1 is what the engineer chose after the split (Option 2) was offered and recommended; the premise note above records that the size is a deliberate, accepted tradeoff. Within that choice, the load bearing calls follow from the forces in Context.

Publish is the writer of the window and attempt limit because no endpoint sets them today and `CanStart` and the available quizzes read already consume them, so making publish the single writer keeps the take path consistent by construction, with no take side change (this is the opposite of spec 0008's archive gap, where the take path had to learn a new rule). Publish validates at least one question because an empty published quiz would show a student a quiz with nothing to answer.

Generation reuses the feedback path's gate, key, and fallback rather than inventing a second AI integration, because security.md section 1's every call has a deterministic fallback and the shared key in section 3 are already satisfied there, and a second pattern would be a second thing to get wrong. `claude-opus-4-8` is chosen for generation while feedback stays on `claude-haiku-4-5`, because the two calls differ in volume and stakes: feedback is one call per wrong answer per attempt and degrades gracefully to a deterministic sentence, so a cheaper faster model fits; generation is low volume (a teacher authors occasionally) and higher stakes, since a bad question is something students are graded on, so the more capable model is worth it. `claude-sonnet-5` is the runner up at roughly half the token price if generation volume ever grows.

Output is validated by hand and per candidate because the community SDK gives no schema guarantee and `QuestionFactory` throws on a bad question: validating each candidate and dropping the invalid ones (rather than constructing and catching) means a partly bad batch yields its good questions instead of failing whole, which is the difference between a useful generator and a fragile one. The empty template fallback (rather than the canned stub) is chosen so that with AI off or failing the teacher still gets a usable starting point they fill in, keeping the deterministic fallback honest without pretending stub text is real content.

Source files are parsed to text and discarded, never stored, because generation only needs the text to build the prompt; not storing the file removes storage, retention, and a large part of the exfiltration surface, leaving only validation and safe parsing. Drafts are a small persisted entity with one pending batch per quiz because server saved (the engineer's choice) means a review survives a refresh, and capping it to one batch per quiz, cleared on accept or discard, bounds the abandoned draft problem without a background sweeper (which foundation section 8 defers).

Questions lock once a quiz has an attempt because the alternative, per attempt snapshotting, is a larger piece of work, and leaving questions editable after attempts would let an edit (now much easier with generation) change what a student was graded against. The lock is the safe default; snapshotting is recorded as a follow up that would also fix the pre existing fresh read risk in the feedback pipeline.

## References

**Project sources**:
- Umbrella spec 0004 (the core loop), which names this as the "(planned) AI quiz generation" child and defines shared contract 1 (AI integration and data minimization).
- Spec 0005 (AI feedback and results), the real Claude pipeline this generation path copies: the community `Anthropic.SDK`, the `Feedback:AiEnabled` plus `Anthropic:ApiKey` gate, the deterministic fallback, and the untrusted output validation.
- Spec 0008 (classroom create and join) for the owner scoped 404 pattern and the rate limit follow up shared here, and for its cross check note that feedback reads questions fresh at grading time (the risk the lock and the snapshot follow up address).
- Spec 0007 (modular monolith), the rule that cross module ids stay plain Guids with no cross schema FK, which fixes the draft's `TeacherId` as a plain Guid.
- `security.md` sections 1 (untrusted output, deterministic fallback), 2 (data minimization for generation), 3 (key server side), 6 (no logging of prompts or responses); `foundation.md` sections 0 (breadth first), 8 (background sweepers deferred), 9 (tenancy from the JWT `UserId`).
- The existing `Quiz` and `Question` entities, `QuestionFactory`, `Quiz.CanStart`, `IQuestionGenerationStrategy` and its stub, and `QuizAppService.GenerateQuestionsAsync` (additive and unvalidated today).

**Practices & standards**:
- Untrusted model output validated against a strict schema before persistence, and capped in length, before it is stored or shown.
- Deterministic fallback on every model call, so an outage degrades the feature and never breaks the loop.
- Validate per item and accept the valid subset, rather than failing a whole batch on one bad element.
- Never store what you can parse and discard: file upload as parse to text, not store the file, to shrink the retention and exfiltration surface.
- Allowlist and cap before parse for uploads, and disable external entity resolution when parsing XML backed formats (docx), to close XXE.
- Lock the graded artifact once it has been used for grading, so a later edit cannot change a past result.
