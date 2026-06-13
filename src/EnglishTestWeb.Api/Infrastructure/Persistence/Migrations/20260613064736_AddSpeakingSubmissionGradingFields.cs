using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishTestWeb.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSpeakingSubmissionGradingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Feedback",
                table: "SpeakingSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "GradedAt",
                table: "SpeakingSubmissions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GraderId",
                table: "SpeakingSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "SpeakingSubmissions",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Feedback",
                table: "SpeakingSubmissions");

            migrationBuilder.DropColumn(
                name: "GradedAt",
                table: "SpeakingSubmissions");

            migrationBuilder.DropColumn(
                name: "GraderId",
                table: "SpeakingSubmissions");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "SpeakingSubmissions");
        }
    }
}
