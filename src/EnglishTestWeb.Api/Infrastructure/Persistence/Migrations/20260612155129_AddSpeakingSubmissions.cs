using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishTestWeb.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSpeakingSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SpeakingSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    HomeworkAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LiveExamSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DraftStoredFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpeakingSubmissions", x => x.Id);
                    table.CheckConstraint("CK_SpeakingSubmissions_ExactlyOneSource", "(HomeworkAssignmentId IS NOT NULL AND LiveExamSessionId IS NULL) OR (HomeworkAssignmentId IS NULL AND LiveExamSessionId IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_SpeakingSubmissions_HomeworkAssignments_HomeworkAssignmentId",
                        column: x => x.HomeworkAssignmentId,
                        principalTable: "HomeworkAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SpeakingSubmissions_LiveExamSessions_LiveExamSessionId",
                        column: x => x.LiveExamSessionId,
                        principalTable: "LiveExamSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SpeakingSubmissions_StoredFiles_DraftStoredFileId",
                        column: x => x.DraftStoredFileId,
                        principalTable: "StoredFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpeakingSubmissions_DraftStoredFileId",
                table: "SpeakingSubmissions",
                column: "DraftStoredFileId");

            migrationBuilder.CreateIndex(
                name: "IX_SpeakingSubmissions_HomeworkAssignmentId",
                table: "SpeakingSubmissions",
                column: "HomeworkAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SpeakingSubmissions_LiveExamSessionId",
                table: "SpeakingSubmissions",
                column: "LiveExamSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SpeakingSubmissions_StudentId_HomeworkAssignmentId",
                table: "SpeakingSubmissions",
                columns: new[] { "StudentId", "HomeworkAssignmentId" },
                unique: true,
                filter: "[HomeworkAssignmentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SpeakingSubmissions_StudentId_LiveExamSessionId",
                table: "SpeakingSubmissions",
                columns: new[] { "StudentId", "LiveExamSessionId" },
                unique: true,
                filter: "[LiveExamSessionId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpeakingSubmissions");
        }
    }
}
