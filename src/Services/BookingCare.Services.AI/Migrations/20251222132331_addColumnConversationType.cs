using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.AI.Migrations
{
    /// <inheritdoc />
    public partial class addColumnConversationType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConversationType",
                table: "ConversationSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConversationType",
                table: "ConversationSessions");
        }
    }
}
