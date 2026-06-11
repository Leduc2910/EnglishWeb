using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishTestWeb.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLiveExamSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LiveExamSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TestTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ScheduledStartAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ScheduledEndAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    OpenedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClosedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiveExamSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LiveExamSessions_AspNetUsers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LiveExamSessions_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LiveExamSessions_TestTemplates_TestTemplateId",
                        column: x => x.TestTemplateId,
                        principalTable: "TestTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LiveExamSessions_ClassId",
                table: "LiveExamSessions",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_LiveExamSessions_TeacherId_ClassId",
                table: "LiveExamSessions",
                columns: new[] { "TeacherId", "ClassId" });

            migrationBuilder.CreateIndex(
                name: "IX_LiveExamSessions_TeacherId_TestTemplateId",
                table: "LiveExamSessions",
                columns: new[] { "TeacherId", "TestTemplateId" });

            migrationBuilder.CreateIndex(
                name: "IX_LiveExamSessions_TestTemplateId",
                table: "LiveExamSessions",
                column: "TestTemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LiveExamSessions");
        }
    }
}
