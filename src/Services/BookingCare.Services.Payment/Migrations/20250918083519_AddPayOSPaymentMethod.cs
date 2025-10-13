using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Payment.Migrations
{
    /// <inheritdoc />
    public partial class AddPayOSPaymentMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "payment_methods",
                columns: new[] { "id", "description", "name", "status" },
                values: new object[] { new Guid("77777777-7777-7777-7777-777777777777"), "Thanh toán qua PayOS", "PAYOS", "ACTIVE" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "payment_methods",
                keyColumn: "id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"));
        }
    }
}
