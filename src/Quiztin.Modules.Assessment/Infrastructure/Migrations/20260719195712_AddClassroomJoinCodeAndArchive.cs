using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quiztin.Modules.Assessment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClassroomJoinCodeAndArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "quiz",
                table: "Classrooms",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                schema: "quiz",
                table: "Classrooms",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "quiz",
                table: "Classrooms",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "JoinCode",
                schema: "quiz",
                table: "Classrooms",
                type: "character varying(6)",
                maxLength: 6,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_ClassroomId",
                schema: "quiz",
                table: "Enrollments",
                column: "ClassroomId");

            migrationBuilder.CreateIndex(
                name: "IX_Classrooms_JoinCode",
                schema: "quiz",
                table: "Classrooms",
                column: "JoinCode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_Classrooms_ClassroomId",
                schema: "quiz",
                table: "Enrollments",
                column: "ClassroomId",
                principalSchema: "quiz",
                principalTable: "Classrooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_Classrooms_ClassroomId",
                schema: "quiz",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_ClassroomId",
                schema: "quiz",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_Classrooms_JoinCode",
                schema: "quiz",
                table: "Classrooms");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                schema: "quiz",
                table: "Classrooms");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "quiz",
                table: "Classrooms");

            migrationBuilder.DropColumn(
                name: "JoinCode",
                schema: "quiz",
                table: "Classrooms");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "quiz",
                table: "Classrooms",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);
        }
    }
}
