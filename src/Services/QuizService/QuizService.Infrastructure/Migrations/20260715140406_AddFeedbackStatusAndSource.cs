using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedbackStatusAndSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FeedbackGeneratedAt",
                table: "QuizAttempts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeedbackStatus",
                table: "QuizAttempts",
                // Default to Pending, not the empty string EF guesses: "" is not a valid
                // FeedbackStatus and would fail to read back into the enum. Any existing
                // attempt reads as Pending; new attempts set it in the app at grading.
                type: "text",
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<string>(
                name: "FeedbackSource",
                table: "QuizAnswers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FeedbackGeneratedAt",
                table: "QuizAttempts");

            migrationBuilder.DropColumn(
                name: "FeedbackStatus",
                table: "QuizAttempts");

            migrationBuilder.DropColumn(
                name: "FeedbackSource",
                table: "QuizAnswers");
        }
    }
}
