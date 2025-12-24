using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.AI.Migrations
{
    /// <inheritdoc />
    public partial class AddLabResultAbnormalIndicatorCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LabResultAbnormalIndicatorCache",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NormalizedKeywords = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    NormalizedText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AbnormalIndicatorsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NormalIndicatorsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SpecialtiesJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Disclaimer = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UsageCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    SuccessRate = table.Column<double>(type: "float", nullable: false, defaultValue: 0.0),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabResultAbnormalIndicatorCache", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LabResult_NormalizedKeywords",
                table: "LabResultAbnormalIndicatorCache",
                column: "NormalizedKeywords");

            migrationBuilder.CreateIndex(
                name: "IX_LabResultAbnormalIndicatorCache_LastUsedAt",
                table: "LabResultAbnormalIndicatorCache",
                column: "LastUsedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LabResultAbnormalIndicatorCache_UsageCount",
                table: "LabResultAbnormalIndicatorCache",
                column: "UsageCount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LabResultAbnormalIndicatorCache");
        }
    }
}
