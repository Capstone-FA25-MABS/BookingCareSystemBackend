using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Payment.Migrations
{
    /// <inheritdoc />
    public partial class AddBankAccountTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bank_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    bank_code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    bank_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    account_number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    account_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    is_default = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bank_accounts", x => x.id);
                    table.CheckConstraint("CK_bank_accounts_account_number_length", "LEN([account_number]) >= 6 AND LEN([account_number]) <= 20");
                    table.CheckConstraint("CK_bank_accounts_account_number_numeric", "[account_number] NOT LIKE '%[^0-9]%'");
                    table.CheckConstraint("CK_bank_accounts_bank_code_length", "LEN([bank_code]) >= 2 AND LEN([bank_code]) <= 10");
                });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bank_accounts");
        }
    }
}
