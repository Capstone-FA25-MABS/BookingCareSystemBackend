using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Payment.Migrations
{
    /// <inheritdoc />
    public partial class ChangeRelationShipRefundHistories : Migration
    {
        private const string IndexName = "IX_refund_histories_payment_id";
        private const string TableName = "refund_histories";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: IndexName,
                table: TableName);



            migrationBuilder.CreateIndex(
                name: IndexName,
                table: TableName,
                column: "payment_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: IndexName,
                table: TableName);



            migrationBuilder.CreateIndex(
                name: IndexName,
                table: TableName,
                column: "payment_id",
                unique: true);
        }
    }
}
