# 0008. Classroom create and join — rationale

This child was written as a single file that leaned on its umbrella, so it has no options analysis of its own. Nothing has been invented to fill the gap; this file says where the reasoning actually is.

- **Why this slice, and the options weighed across the whole loop:** the umbrella's [rationale.md](../rationale.md).
- **Why this slice is shaped the way it is:** the `## Rationale` section inside [index.md](index.md).
- **The decisions that carried the most weight,** recorded in the `progress-log.md` entry *Classroom create + join built — FR7 is real*: joining by a short code with the link wrapping that same code, so there is one secret to rotate and no server side base URL to configure; **strict create, open join** (the Teacher role to create, any authenticated user to join, because the code is the capability); delete modelled as a **reversible archive** so graded student history is never destroyed; and a new `IClassroomRepository` as a peer of `IQuizAttemptRepository` rather than more methods on `IQuizRepository`.
- **A deliberate deviation from the spec's wording:** the migration was written **additively** instead of regenerating `InitialCreate`, because the deployed Railway database already had `InitialCreate` applied and regenerating it would have broken production.
- **What a second model caught before the build:** archive was unenforced on the take path (`GetAvailableForStudentAsync` and `StartQuizAsync` never checked it), fixed and tested during the build.

Specs written from 0009 onward keep their own `rationale.md` with the options laid out. This one is retrofitted for shape, on 2026-07-23.
