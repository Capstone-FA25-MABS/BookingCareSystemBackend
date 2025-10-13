using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Payment.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusToPaymentMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "payment_methods",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "payment_methods",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "status",
                value: "ACTIVE");

            migrationBuilder.UpdateData(
                table: "payment_methods",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "status",
                value: "ACTIVE");

            migrationBuilder.UpdateData(
                table: "payment_methods",
                keyColumn: "id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "status",
                value: "ACTIVE");

            migrationBuilder.UpdateData(
                table: "payment_methods",
                keyColumn: "id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "status",
                value: "ACTIVE");

            migrationBuilder.UpdateData(
                table: "payment_methods",
                keyColumn: "id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "status",
                value: "ACTIVE");

            migrationBuilder.UpdateData(
                table: "payment_methods",
                keyColumn: "id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                column: "status",
                value: "ACTIVE");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payment_methods_status",
                table: "payment_methods",
                sql: "[status] IN ('ACTIVE', 'INACTIVE')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_payment_methods_status",
                table: "payment_methods");

            migrationBuilder.DropColumn(
                name: "status",
                table: "payment_methods");
        }
    }
}
