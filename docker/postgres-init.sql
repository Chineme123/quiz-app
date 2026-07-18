-- One database for the modular monolith (spec 0007). Runs once, against the default
-- 'postgres' database, when the volume is first initialised. The app owns the schemas
-- inside it: `identity` and `quiz`, each with its own EF migration history.
CREATE DATABASE quiztin;
