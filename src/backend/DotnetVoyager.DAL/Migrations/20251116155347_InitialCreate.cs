using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotnetVoyager.DAL.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnalysisStatuses",
                columns: table => new
                {
                    AnalysisId = table.Column<string>(type: "TEXT", nullable: false),
                    OverallStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    OriginalFileName = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisStatuses", x => x.AnalysisId);
                });

            migrationBuilder.CreateTable(
                name: "AnalysisSteps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AnalysisId = table.Column<string>(type: "TEXT", nullable: false),
                    StepName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    StartedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RetryCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalysisSteps_AnalysisStatuses_AnalysisId",
                        column: x => x.AnalysisId,
                        principalTable: "AnalysisStatuses",
                        principalColumn: "AnalysisId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisSteps_AnalysisId_StepName",
                table: "AnalysisSteps",
                columns: new[] { "AnalysisId", "StepName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisSteps_Status_AnalysisId",
                table: "AnalysisSteps",
                columns: new[] { "Status", "AnalysisId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalysisSteps");

            migrationBuilder.DropTable(
                name: "AnalysisStatuses");
        }
    }
}
