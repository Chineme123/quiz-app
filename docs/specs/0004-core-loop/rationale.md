# 0004. The core loop — rationale

Decision record for the umbrella. `/develop` does not need this file; it is here for humans and for `/architect` on a later update.

## Context

> ⚠️ Premise note (scope): the topic, the whole core loop, spans five or more independently buildable decisions (classroom create and join, quiz authoring with AI, take and feedback, results, and the shared AI contract). One spec captures one decision, so this is an umbrella. Only the first child, the AI feedback thread, is fully designed now; the rest are named in the manifest and designed in later passes.

> ⚠️ Premise note (provider): the invoking prompt asked for real openAI feedback, but foundation locks the AI provider to Anthropic Claude in two places, 7 #6 (AI in v1 is Claude) and 7 #17 (the security authority: model access is Anthropic API only), and the existing strategy seams and fallback assume it. The engineer confirmed Claude, so no source of truth changes. Had OpenAI been intended, it would have required changing foundation 7 #6 and 7 #17 and the model egress rules in security.md first.

> ⚠️ Premise note (read side): the first child serves the student results read from QuizService rather than ResultService, a deliberate temporary deviation from foundation 7 #8 (ResultService owns reads), taken to keep the wedge thread thin. It is tracked, and the read moves to ResultService when that child is built.

> ⚠️ Premise note (stale drift): foundation 10 and `build-graph.md` still describe scoring as fake, but the code has already redesigned the scoring contract and it is real (`IScoringStrategy.Score(attempt, questions)` receives the questions with their correct answers, and `PointsScoringStrategy` grades against them). The wedge child carries a Follow-up to reconcile these lines.

The core loop is the product's keystone and its differentiator at once. The platform, auth, profile, and landing page are built and production grade, but a human cannot yet run the loop: there are no classroom create or join endpoints, ResultService is an empty scaffold, and the AI layer is a stub with no client of any kind. The loop dead ends at submission. The forces that shape how to attack it: a solo developer with open time but real risk in the one unproven area (the AI integration and the async feedback path); a locked stack, provider, and security posture that the design must honor rather than revisit; and a data minimization rule that must hold by construction because student academic answers cross the boundary to a third party model.

## Options considered

### Option 1: One pass over the whole loop

Design all five children in a single sitting.

**Pros**:
- One coherent design pass; nothing deferred.

**Cons**:
- A very long interview and a sprawling spec that is hard to keep scannable.
- The riskiest part (the AI) is designed alongside low risk CRUD, diluting focus, and early children would likely churn the design as facts surface during the build.

### Option 2: Umbrella plus the wedge child first (chosen)

Capture the shared contracts in the umbrella, fully design the highest value and highest risk child (AI feedback plus the student read), and sequence the rest.

**Pros**:
- De risks the differentiator first and proves the shared contracts on real code before the loop widens.
- Matches the engineer's thinnest vertical intent, and keeps each spec small and scannable.

**Cons**:
- The full loop lands over several passes rather than at once.
- The wedge thread bends the breadth first stance and the read side location, both temporarily.

### Option 3: Umbrella plus dependency order from the root

Same umbrella, but design classroom create and join first (UC2, UC3), then quiz, take, results.

**Pros**:
- Builds in the natural hard dependency order, so no seeded preconditions and no read location deviation.

**Cons**:
- Front loads low risk CRUD and defers the risky, differentiating AI work, so the wedge, the thing most in doubt, is proven last.

## Rationale

Option 2 wins because the binding risk in this loop is the AI integration and the async feedback path, not the classroom or quiz CRUD, which is well understood and partly built. Proving the wedge end to end first, on the real code and the real strategy seams, retires that risk early and makes the public AI claim honest sooner. It also matches how the engineer chose to slice every question in the interview, toward the thinnest vertical that a human can see working. The runner up, dependency order, is cleaner architecturally (no seeded preconditions, no read location deviation) but it defers the one part most worth de risking to the very end, which is the wrong trade when the differentiator is the open question.

Within the wedge child, the load bearing calls were: Claude as the provider (locked and confirmed); the student read served from QuizService now to avoid the heaviest piece, the cross service projection, before the wedge is proven; asynchronous feedback via a background queue and hosted service so submit never waits on the model; and the Anthropic .NET SDK as the client. The SDK is the engineer's explicit choice over a typed HttpClient wrapper; it cuts against the few dependencies house style (foundation 7 #21), and that tradeoff is recorded as a Follow-up in the child spec. Data minimization still holds, because the code builds the exact message content the SDK sends.
