using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.AI.Migrations
{
    /// <inheritdoc />
    public partial class addcachequestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConversationContextKeywords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Keyword = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Synonyms = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationContextKeywords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SymptomQuestionCache",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InitialSymptom = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ConversationContext = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NormalizedKeywords = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    QuestionNumber = table.Column<int>(type: "int", nullable: false),
                    Question = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    UsageCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    SuccessRate = table.Column<double>(type: "float", nullable: false, defaultValue: 0.0),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SymptomQuestionCache", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConversationContextKeywords_Category",
                table: "ConversationContextKeywords",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationContextKeywords_Keyword",
                table: "ConversationContextKeywords",
                column: "Keyword",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NormalizedKeywords_QuestionNumber",
                table: "SymptomQuestionCache",
                columns: new[] { "NormalizedKeywords", "QuestionNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_SymptomQuestionCache_InitialSymptom",
                table: "SymptomQuestionCache",
                column: "InitialSymptom");

            migrationBuilder.CreateIndex(
                name: "IX_SymptomQuestionCache_LastUsedAt",
                table: "SymptomQuestionCache",
                column: "LastUsedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SymptomQuestionCache_UsageCount",
                table: "SymptomQuestionCache",
                column: "UsageCount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConversationContextKeywords");

            migrationBuilder.DropTable(
                name: "SymptomQuestionCache");
        }
    }
}
