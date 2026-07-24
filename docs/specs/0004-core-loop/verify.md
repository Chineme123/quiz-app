# Verify: the core loop (umbrella 0004)

The umbrella itself has no verify steps of its own. Every step belongs to one of its children, and each child now keeps its own `verify.md` beside its spec:

| Child | Verify steps | Last run |
|---|---|---|
| 0005 AI feedback and student results | [verify.md](0005-ai-feedback-and-results/verify.md) | `/check verify` run, defects found and fixed |
| 0006 Take quiz screen | [verify.md](0006-take-quiz-screen/verify.md) | `/check verify` run, two defects found that 82 green tests missed |
| 0008 Classroom create and join | [verify.md](0008-classroom-create-join/verify.md) | never run independently; a live end to end walk was done during the build |
| 0009 Quiz authoring, generation, and publish | [verify.md](0009-quiz-authoring/verify.md) | never run; task 7 of the build plan **is** the verify |

Until 2026-07-23 this file held the 0005 and 0006 steps directly, concatenated, while 0008 had none anywhere and 0009 kept its own. The steps were moved to their children unchanged; this file is kept as the way in, because earlier `progress-log.md` entries point at this path.

**Verify the loop end to end** (what no single child covers, because it spans all of them): a teacher registers, creates a classroom, authors and publishes a quiz, a student registers, joins by code, takes it, and both sides read the result with its AI written feedback, all without touching the seeder. That walk is the umbrella's real acceptance test and belongs here once every child is built.
