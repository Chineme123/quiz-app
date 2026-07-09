-- Database-per-service on one Postgres instance (foundation §7 #10).
-- Runs once on first container start, against the default 'postgres' database.
CREATE DATABASE authdb;
CREATE DATABASE userdb;
CREATE DATABASE quizdb;
CREATE DATABASE resultdb;
CREATE DATABASE notificationdb;
