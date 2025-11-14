using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Auth.Migrations
{
    /// <inheritdoc />
    public partial class Add2FAFieldsToAccount : Migration
    {
        private const string TABLE_NAME_ASP_NET_USERS = "AspNetUsers";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TwoFactorBackupCodes",
                table: TABLE_NAME_ASP_NET_USERS,
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TwoFactorEnabledAt",
                table: TABLE_NAME_ASP_NET_USERS,
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TwoFactorSecretKey",
                table: TABLE_NAME_ASP_NET_USERS,
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TwoFactorBackupCodes",
                table: TABLE_NAME_ASP_NET_USERS);

            migrationBuilder.DropColumn(
                name: "TwoFactorEnabledAt",
                table: TABLE_NAME_ASP_NET_USERS);

            migrationBuilder.DropColumn(
                name: "TwoFactorSecretKey",
                table: TABLE_NAME_ASP_NET_USERS);
        }
    }
}
