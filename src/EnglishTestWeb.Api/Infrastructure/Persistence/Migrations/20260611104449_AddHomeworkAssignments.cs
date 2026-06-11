using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishTestWeb.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHomeworkAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HomeworkAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TestTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DeadlineAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TimeLimitMinutes = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomeworkAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HomeworkAssignments_AspNetUsers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HomeworkAssignments_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HomeworkAssignments_TestTemplates_TestTemplateId",
                        column: x => x.TestTemplateId,
                        principalTable: "TestTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HomeworkAssignments_ClassId",
                table: "HomeworkAssignments",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_HomeworkAssignments_TeacherId_ClassId",
                table: "HomeworkAssignments",
                columns: new[] { "TeacherId", "ClassId" });

            migrationBuilder.CreateIndex(
                name: "IX_HomeworkAssignments_TeacherId_TestTemplateId",
                table: "HomeworkAssignments",
                columns: new[] { "TeacherId", "TestTemplateId" });

            migrationBuilder.CreateIndex(
                name: "IX_HomeworkAssignments_TestTemplateId",
                table: "HomeworkAssignments",
                column: "TestTemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HomeworkAssignments");
        }
    }
}
