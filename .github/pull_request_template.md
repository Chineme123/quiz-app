## Summary

<!-- What does this change do, and why? Keep it tight. -->

## Linked spec / use case

<!-- e.g. spec 0002 phase N · docs/specs/000X · UCn · or "n/a" -->

## How it was verified

<!-- Build/tests/CI, a local run, curl output, a screenshot — how you know it works. -->

## Definition of done (from CLAUDE.md)

- [ ] `context/progress-log.md` entry added (mandatory for real work)
- [ ] If a decision changed a context file, that file is updated too (the drift rule)
- [ ] Build + tests green (`dotnet build QuizApp.sln`, `dotnet test`, and `frontend/` checks if touched)
- [ ] No secrets committed (DB password, JWT key, API keys stay in env / user-secrets)
