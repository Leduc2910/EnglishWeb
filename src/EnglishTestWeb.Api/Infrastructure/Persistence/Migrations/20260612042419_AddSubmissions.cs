using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishTestWeb.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Submissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    HomeworkAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LiveExamSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AnswerKeyVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Submissions", x => x.Id);
                    table.CheckConstraint("CK_Submissions_ExactlyOneSource", "([HomeworkAssignmentId] IS NOT NULL AND [LiveExamSessionId] IS NULL) OR ([HomeworkAssignmentId] IS NULL AND [LiveExamSessionId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Submissions_AnswerKeyVersions_AnswerKeyVersionId",
                        column: x => x.AnswerKeyVersionId,
                        principalTable: "AnswerKeyVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Submissions_HomeworkAssignments_HomeworkAssignmentId",
                        column: x => x.HomeworkAssignmentId,
                        principalTable: "HomeworkAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Submissions_LiveExamSessions_LiveExamSessionId",
                        column: x => x.LiveExamSessionId,
                        principalTable: "LiveExamSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_AnswerKeyVersionId",
                table: "Submissions",
                column: "AnswerKeyVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_HomeworkAssignmentId",
                table: "Submissions",
                column: "HomeworkAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_LiveExamSessionId",
                table: "Submissions",
                column: "LiveExamSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_StudentId_HomeworkAssignmentId",
                table: "Submissions",
                columns: new[] { "StudentId", "HomeworkAssignmentId" },
                unique: true,
                filter: "[HomeworkAssignmentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_StudentId_LiveExamSessionId",
                table: "Submissions",
                columns: new[] { "StudentId", "LiveExamSessionId" },
                unique: true,
                filter: "[LiveExamSessionId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Submissions");
        }
    }
}
