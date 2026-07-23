using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quiztin.Modules.Assessment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGeneratedQuestionDrafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GeneratedQuestionDrafts",
                schema: "quiz",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuizId = table.Column<Guid>(type: "uuid", nullable: false),
                    Candidates = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneratedQuestionDrafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GeneratedQuestionDrafts_Quizzes_QuizId",
                        column: x => x.QuizId,
                        principalSchema: "quiz",
                        principalTable: "Quizzes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedQuestionDrafts_QuizId",
                schema: "quiz",
                table: "GeneratedQuestionDrafts",
                column: "QuizId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GeneratedQuestionDrafts",
                schema: "quiz");
        }
    }
}
