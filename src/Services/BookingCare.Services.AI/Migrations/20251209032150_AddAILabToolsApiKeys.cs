using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.AI.Migrations
{
    /// <inheritdoc />
    public partial class AddAILabToolsApiKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AILabToolsApiKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApiKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UsageCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    MaxUsageCount = table.Column<int>(type: "int", nullable: false, defaultValue: 10),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AILabToolsApiKeys", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AILabToolsApiKeys_IsActive",
                table: "AILabToolsApiKeys",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AILabToolsApiKeys_LastUsedAt",
                table: "AILabToolsApiKeys",
                column: "LastUsedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AILabToolsApiKeys_UsageCount",
                table: "AILabToolsApiKeys",
                column: "UsageCount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AILabToolsApiKeys");
        }
    }
}
