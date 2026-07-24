# 0006. Take quiz screen — rationale

This child was written as a single file that leaned on its umbrella, so it has no options analysis of its own. Nothing has been invented to fill the gap; this file says where the reasoning actually is.

- **Why this slice, and the options weighed across the whole loop:** the umbrella's [rationale.md](../rationale.md).
- **Why this slice is shaped the way it is:** the `## Rationale` section inside [index.md](index.md).
- **The two decisions that changed the foundation,** agreed deliberately while designing this child rather than discovered late: an expired attempt now **grades the answers already saved** instead of abandoning (foundation §69 trigger 1), and **save draft with auto save was pulled into v1** (foundation §8), because a one shot timed quiz that loses work to a refresh is punishing in a product built to help people learn. Both are recorded in `foundation.md` and in the `progress-log.md` entry *Reconcile foundation with spec 0006's two overrides*.
- **What was decided while building it:** the `progress-log.md` entries *Take quiz backend* (tasks 1 to 8) and *Take quiz screen* (tasks 9 to 14), plus *Two defects `/check verify` caught on 0006 that 82 green tests missed* — the clearest evidence in this project that a green suite is not a verify.

Specs written from 0009 onward keep their own `rationale.md` with the options laid out. This one is retrofitted for shape, on 2026-07-23.
