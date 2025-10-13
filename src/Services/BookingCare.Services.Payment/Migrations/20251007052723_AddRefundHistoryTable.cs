using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Payment.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundHistoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_payment_methods_name",
                table: "payment_methods");

            migrationBuilder.DropIndex(
                name: "IX_bank_accounts_user_id",
                table: "bank_accounts");

            migrationBuilder.DropIndex(
                name: "IX_bank_accounts_user_id_is_default",
                table: "bank_accounts");

            migrationBuilder.DropIndex(
                name: "UQ_bank_accounts_bank_code_account_number",
                table: "bank_accounts");

            migrationBuilder.CreateTable(
                name: "refund_histories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    bank_account_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    transfer_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    payment_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    refund_amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    refund_reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    staff_notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    processed_by_staff_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refund_histories", x => x.id);
                    table.CheckConstraint("CK_refund_histories_refund_amount_positive", "[refund_amount] > 0");
                    table.CheckConstraint("CK_refund_histories_status", "[status] IN ('WAITING', 'PENDING', 'COMPLETED')");
                    table.CheckConstraint("CK_refund_histories_transfer_date_completed", "([status] = 'COMPLETED' AND [transfer_date] IS NOT NULL) OR ([status] != 'COMPLETED')");
                    table.ForeignKey(
                        name: "FK_refund_histories_bank_account_id",
                        column: x => x.bank_account_id,
                        principalTable: "bank_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_refund_histories_payment_id",
                        column: x => x.payment_id,
                        principalTable: "payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_refund_histories_bank_account_id",
                table: "refund_histories",
                column: "bank_account_id");

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

            migrationBuilder.CreateIndex(
                name: "UQ_refund_histories_payment_id",
                table: "refund_histories",
                column: "payment_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "refund_histories");

            migrationBuilder.CreateIndex(
                name: "UQ_payment_methods_name",
                table: "payment_methods",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bank_accounts_user_id",
                table: "bank_accounts",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_bank_accounts_user_id_is_default",
                table: "bank_accounts",
                columns: new[] { "user_id", "is_default" });

            migrationBuilder.CreateIndex(
                name: "UQ_bank_accounts_bank_code_account_number",
                table: "bank_accounts",
                columns: new[] { "bank_code", "account_number" },
                unique: true);
        }
    }
}
