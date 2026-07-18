# 0007. Modular monolith — rationale

Decision record. `/develop` does not need this file.

## Context

> ⚠️ Premise note (right problem): the profile-save 500 could have been patched in place
> (provision the missing user row on first save). But the bug is a *symptom* of premature
> microservices: the same user split across `authdb` and `userdb`, with a required foreign
> key that nothing could satisfy. Patching the symptom leaves the cause. The engineer chose
> to fix the cause.

The system was five .NET microservices with database-per-service behind a YARP gateway: 24
projects, five databases (`authdb`, `userdb`, `quizdb`, `resultdb`, `notificationdb`).
Exploration confirmed the "distributed system" shared nothing at runtime except a JWT
convention and one Postgres server: zero service-to-service HTTP, no message bus, no shared
code, no cross-service project references. Two of the five services (Result, Notification)
were empty `dotnet new` scaffolds; QuizService was the whole product and already did
everything in-process. The forces: a solo developer, no traffic, no data to preserve, and a
real correctness bug caused directly by the split identity.

## Options considered

### Option 1: Keep microservices, patch the bug
Provision the `userdb.Users` row on first profile save (or on registration via an event).

**Pros**: smallest change; keeps the current shape.
**Cons**: pays the five-deployable, five-database, cross-service tax with no traffic to
justify it; the identity stays duplicated across databases; every future feature keeps
crossing service boundaries by convention with no integrity.

### Option 2: Modular monolith (chosen)
One host, two module boundaries, one database with a schema per module.

**Pros**: fixes the bug by construction (one users table, real FK); removes the gateway,
the two stub services, the per-service JWT config, and the cross-service seams; far simpler
to build and operate; keeps clean module seams so a module can still be split out later.
**Cons**: one failure and scaling unit; layering within a module is by convention, not
compiler-enforced; the full Quiztin rename reverses foundation §7 #4.

### Option 3: Merge only Auth+User, keep the rest split
Fix the identity duplication but keep separate Quiz/Result deployables and the gateway.

**Pros**: fixes the bug; less sweeping.
**Cons**: keeps most of the tax (the gateway, multiple deployables, database-per-service)
for no benefit at this stage; a half-measure that would likely be finished later anyway.

## Rationale

Option 2 wins because the binding constraint is stage, not scale: a solo developer with no
traffic gets nothing from five deployables and five databases, and pays for all of it in
operational and cognitive overhead, plus a correctness bug the split *caused*. Collapsing to
one process against one database removes the tax and fixes the bug at the same time, while
per-module projects and per-module Postgres schemas preserve exactly the boundary that
matters if a module ever needs to be split back out ("build like microservices, not for
microservices"). The runtime coupling was already zero, so the collapse was additive, not a
de-tangling job, which made it low risk. Cross-module references stay plain Guids rather than
real foreign keys precisely to keep a module splittable; integrity is added only where both
tables live in the same module.
