using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.AI.Migrations
{
    /// <inheritdoc />
    public partial class AddDermatologyDiseaseCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DermatologyDiseaseCache",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnglishName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    VietnameseName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AdviceJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UsageCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    SuccessRate = table.Column<double>(type: "float", nullable: false, defaultValue: 0.0),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DermatologyDiseaseCache", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dermatology_EnglishName",
                table: "DermatologyDiseaseCache",
                column: "EnglishName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DermatologyDiseaseCache_LastUsedAt",
                table: "DermatologyDiseaseCache",
                column: "LastUsedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DermatologyDiseaseCache_UsageCount",
                table: "DermatologyDiseaseCache",
                column: "UsageCount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DermatologyDiseaseCache");
        }
    }
}
