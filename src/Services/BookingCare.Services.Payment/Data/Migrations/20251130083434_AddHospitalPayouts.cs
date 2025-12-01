using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Payment.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHospitalPayouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "hospital_payouts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    hospital_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    bank_account_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    period_start = table.Column<DateTime>(type: "datetime2", nullable: false),
                    period_end = table.Column<DateTime>(type: "datetime2", nullable: false),
                    total_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    appointment_count = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    processed_by_admin_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    processed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hospital_payouts", x => x.id);
                    table.CheckConstraint("CK_hospital_payouts_appointment_count_positive", "[appointment_count] > 0");
                    table.CheckConstraint("CK_hospital_payouts_period_valid", "[period_start] <= [period_end]");
                    table.CheckConstraint("CK_hospital_payouts_status", "[status] IN ('PENDING', 'COMPLETED')");
                    table.CheckConstraint("CK_hospital_payouts_total_amount_positive", "[total_amount] > 0");
                    table.ForeignKey(
                        name: "FK_hospital_payouts_bank_account_id",
                        column: x => x.bank_account_id,
                        principalTable: "bank_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_hospital_payouts_bank_account_id",
                table: "hospital_payouts",
                column: "bank_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_hospital_payouts_hospital_id",
                table: "hospital_payouts",
                column: "hospital_id");

            migrationBuilder.CreateIndex(
                name: "IX_hospital_payouts_hospital_period",
                table: "hospital_payouts",
                columns: new[] { "hospital_id", "period_start", "period_end" });

            migrationBuilder.CreateIndex(
                name: "IX_hospital_payouts_status",
                table: "hospital_payouts",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "hospital_payouts");
        }
    }
}
