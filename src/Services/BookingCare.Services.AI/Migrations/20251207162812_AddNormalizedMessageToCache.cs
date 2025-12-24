using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.AI.Migrations
{
    /// <inheritdoc />
    public partial class AddNormalizedMessageToCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NormalizedMessage",
                table: "SymptomQuestionCache",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NormalizedMessage_QuestionNumber",
                table: "SymptomQuestionCache",
                columns: new[] { "NormalizedMessage", "QuestionNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NormalizedMessage_QuestionNumber",
                table: "SymptomQuestionCache");

            migrationBuilder.DropColumn(
                name: "NormalizedMessage",
                table: "SymptomQuestionCache");
        }
    }
}
