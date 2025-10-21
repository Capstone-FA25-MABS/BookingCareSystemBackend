using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Payment.Migrations
{
    /// <inheritdoc />
    public partial class ChangeRelationShipRefundHistories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_refund_histories_payment_id",
                table: "refund_histories");



            migrationBuilder.CreateIndex(
                name: "IX_refund_histories_payment_id",
                table: "refund_histories",
                column: "payment_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_refund_histories_payment_id",
                table: "refund_histories");



            migrationBuilder.CreateIndex(
                name: "IX_refund_histories_payment_id",
                table: "refund_histories",
                column: "payment_id",
                unique: true);
        }
    }
}
