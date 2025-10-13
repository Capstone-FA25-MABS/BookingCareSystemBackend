using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Payment.Migrations
{
    /// <inheritdoc />
    public partial class _20251008_RenameClinicIdToHospitalId_AddHospitalIdToRefundHistories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_refund_histories_status",
                table: "refund_histories");

            migrationBuilder.DropIndex(
                name: "IX_refund_histories_user_id",
                table: "refund_histories");

            migrationBuilder.DropIndex(
                name: "IX_refund_histories_user_status",
                table: "refund_histories");

            migrationBuilder.RenameIndex(
                name: "UQ_refund_histories_payment_id",
                table: "refund_histories",
                newName: "IX_refund_histories_payment_id");

            migrationBuilder.RenameColumn(
                name: "clinic_id",
                table: "payments",
                newName: "hospital_id");

            migrationBuilder.AddColumn<Guid>(
                name: "hospital_id",
                table: "refund_histories",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "hospital_id",
                table: "refund_histories");

            migrationBuilder.RenameIndex(
                name: "IX_refund_histories_payment_id",
                table: "refund_histories",
                newName: "UQ_refund_histories_payment_id");

            migrationBuilder.RenameColumn(
                name: "hospital_id",
                table: "payments",
                newName: "clinic_id");

            migrationBuilder.CreateIndex(
                name: "IX_refund_histories_status",
                table: "refund_histories",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_refund_histories_user_id",
                table: "refund_histories",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_refund_histories_user_status",
                table: "refund_histories",
                columns: new[] { "user_id", "status" });
        }
    }
}
