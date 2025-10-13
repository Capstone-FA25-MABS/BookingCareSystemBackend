using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BookingCare.Services.Payment.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payment_methods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_methods", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    appointment_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    clinic_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    patient_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    transaction_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    payment_method_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    status = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.id);
                    table.CheckConstraint("CK_payments_status", "[status] IN ('PENDING', 'COMPLETED', 'FAILED', 'REFUNDED')");
                    table.CheckConstraint("CK_payments_transaction_type", "[transaction_type] IN ('APPOINTMENT', 'SUBSCRIPTION')");
                    table.ForeignKey(
                        name: "FK_payments_payment_method_id",
                        column: x => x.payment_method_id,
                        principalTable: "payment_methods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "payment_methods",
                columns: new[] { "id", "description", "name" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "Thanh toán bằng tiền mặt", "CASH" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "Thanh toán bằng thẻ tín dụng", "CREDIT_CARD" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "Chuyển khoản ngân hàng", "BANK_TRANSFER" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "Thanh toán qua MoMo", "MOMO" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "Thanh toán qua ZaloPay", "ZALOPAY" },
                    { new Guid("66666666-6666-6666-6666-666666666666"), "Thanh toán qua VNPay", "VNPAY" }
                });

            migrationBuilder.CreateIndex(
                name: "UQ_payment_methods_name",
                table: "payment_methods",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payments_payment_method_id",
                table: "payments",
                column: "payment_method_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "payment_methods");
        }
    }
}
