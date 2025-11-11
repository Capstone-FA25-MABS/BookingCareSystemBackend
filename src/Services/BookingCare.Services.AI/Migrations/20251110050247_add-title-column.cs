using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.AI.Migrations
{
    /// <inheritdoc />
    public partial class addtitlecolumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "ConversationSessions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                table: "ConversationSessions");
        }
    }
}
