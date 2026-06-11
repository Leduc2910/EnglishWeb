using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishTestWeb.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAnswerKeyVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnswerKeyVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ScoringMode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    QuestionCount = table.Column<int>(type: "int", nullable: false),
                    TotalScore = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: true),
                    RowsJson = table.Column<string>(type: "nvarchar(max)", maxLength: -1, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnswerKeyVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnswerKeyVersions_TestTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "TestTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnswerKeyVersions_TemplateId",
                table: "AnswerKeyVersions",
                column: "TemplateId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnswerKeyVersions");
        }
    }
}
