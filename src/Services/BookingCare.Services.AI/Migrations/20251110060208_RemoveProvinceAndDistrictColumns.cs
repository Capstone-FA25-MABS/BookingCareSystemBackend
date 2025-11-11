using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.AI.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProvinceAndDistrictColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DistrictId",
                table: "ConversationSessions");

            migrationBuilder.DropColumn(
                name: "ProvinceId",
                table: "ConversationSessions");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "ConversationSessions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "ConversationSessions",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "DistrictId",
                table: "ConversationSessions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProvinceId",
                table: "ConversationSessions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}
