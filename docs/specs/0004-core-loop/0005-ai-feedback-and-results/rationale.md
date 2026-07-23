# 0005. AI feedback and student results — rationale

This child was written as a single file that leaned on its umbrella, so it has no options analysis of its own. Nothing has been invented to fill the gap; this file says where the reasoning actually is.

- **Why this slice, and the options weighed across the whole loop:** the umbrella's [rationale.md](../rationale.md).
- **Why this slice is shaped the way it is:** the `## Rationale` section inside [index.md](index.md) — thin and vertical to prove the riskiest path first, the AI strategy sitting behind the `IFeedbackStrategy` seam that already existed, the read served from QuizService rather than standing up ResultService, and the model kept off the submit path.
- **What was decided while building it,** which is where the real trade offs ended up: the `progress-log.md` entries *AI feedback + student results — the core-loop wedge*, then the four follow ups it produced (the `/check` review, the polling leak, `IHttpClientFactory` pooling, submit idempotency, and token scoping).

Specs written from 0009 onward keep their own `rationale.md` with the options laid out. This one is retrofitted for shape, on 2026-07-23.
