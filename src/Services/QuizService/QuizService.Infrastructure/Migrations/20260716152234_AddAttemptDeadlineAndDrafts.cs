using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAttemptDeadlineAndDrafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AbandonReason",
                table: "QuizAttempts",
                type: "text",
                nullable: true);

            // An empty object, not EF's guessed empty string: '' is not valid jsonb and Postgres
            // rejects the DDL outright. The map is always present, empty until the student
            // answers something.
            migrationBuilder.AddColumn<string>(
                name: "DraftAnswers",
                table: "QuizAttempts",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            // Backfill only: every attempt from here on gets its real deadline pinned at Start.
            // EF guessed DateTime year 1 with Kind=Unspecified, which Npgsql refuses to write to
            // a timestamptz. The epoch, in UTC, says what is true of any attempt that predates
            // this feature: its time is long up, so it takes no new drafts. Submitting one still
            // grades whatever it holds, because expiry grades rather than abandons (spec 0006).
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "QuizAttempts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AbandonReason",
                table: "QuizAttempts");

            migrationBuilder.DropColumn(
                name: "DraftAnswers",
                table: "QuizAttempts");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "QuizAttempts");
        }
    }
}
