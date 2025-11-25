using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Hospital.Migrations
{
    /// <inheritdoc />
    public partial class AddTableContractSignature : Migration
    {
        // Constants for table and column names to avoid magic strings
        private const string TABLE_HOSPITAL_REGISTRATIONS = "hospital_registrations";
        private const string TABLE_ADMIN_SIGNATURES = "admin_signatures";
        private const string TABLE_CONTRACT_SIGNING_TOKENS = "contract_signing_tokens";
        private const string COLUMN_ADMIN_SIGNATURE_ID = "admin_signature_id";
        private const string TYPE_UNIQUEIDENTIFIER = "uniqueidentifier";
        private const string TYPE_DATETIME2 = "datetime2";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: COLUMN_ADMIN_SIGNATURE_ID,
                table: TABLE_HOSPITAL_REGISTRATIONS,
                type: TYPE_UNIQUEIDENTIFIER,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "contract_draft_file",
                table: TABLE_HOSPITAL_REGISTRATIONS,
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "contract_number",
                table: TABLE_HOSPITAL_REGISTRATIONS,
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "hospital_signature",
                table: TABLE_HOSPITAL_REGISTRATIONS,
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "signed_at",
                table: TABLE_HOSPITAL_REGISTRATIONS,
                type: TYPE_DATETIME2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: TABLE_ADMIN_SIGNATURES,
                columns: table => new
                {
                    id = table.Column<Guid>(type: TYPE_UNIQUEIDENTIFIER, nullable: false),
                    admin_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    full_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    position = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    signature_image_url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: TYPE_DATETIME2, nullable: false, defaultValueSql: "GETDATE()"),
                    updated_at = table.Column<DateTime>(type: TYPE_DATETIME2, nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_signatures", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: TABLE_CONTRACT_SIGNING_TOKENS,
                columns: table => new
                {
                    id = table.Column<Guid>(type: TYPE_UNIQUEIDENTIFIER, nullable: false),
                    registration_id = table.Column<Guid>(type: TYPE_UNIQUEIDENTIFIER, nullable: false),
                    token = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    expires_at = table.Column<DateTime>(type: TYPE_DATETIME2, nullable: false),
                    is_used = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    used_at = table.Column<DateTime>(type: TYPE_DATETIME2, nullable: true),
                    signed_from_ip = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: TYPE_DATETIME2, nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contract_signing_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_contract_signing_tokens_hospital_registrations_registration_id",
                        column: x => x.registration_id,
                        principalTable: TABLE_HOSPITAL_REGISTRATIONS,
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_hospital_registrations_admin_signature_id",
                table: TABLE_HOSPITAL_REGISTRATIONS,
                column: COLUMN_ADMIN_SIGNATURE_ID);

            migrationBuilder.CreateIndex(
                name: "IX_admin_signatures_admin_id",
                table: TABLE_ADMIN_SIGNATURES,
                column: "admin_id");

            migrationBuilder.CreateIndex(
                name: "IX_contract_signing_tokens_registration_id",
                table: TABLE_CONTRACT_SIGNING_TOKENS,
                column: "registration_id");

            migrationBuilder.CreateIndex(
                name: "IX_contract_signing_tokens_token",
                table: TABLE_CONTRACT_SIGNING_TOKENS,
                column: "token",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_hospital_registrations_admin_signatures_admin_signature_id",
                table: TABLE_HOSPITAL_REGISTRATIONS,
                column: COLUMN_ADMIN_SIGNATURE_ID,
                principalTable: TABLE_ADMIN_SIGNATURES,
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_hospital_registrations_admin_signatures_admin_signature_id",
                table: TABLE_HOSPITAL_REGISTRATIONS);

            migrationBuilder.DropTable(
                name: TABLE_ADMIN_SIGNATURES);

            migrationBuilder.DropTable(
                name: TABLE_CONTRACT_SIGNING_TOKENS);

            migrationBuilder.DropIndex(
                name: "IX_hospital_registrations_admin_signature_id",
                table: TABLE_HOSPITAL_REGISTRATIONS);

            migrationBuilder.DropColumn(
                name: COLUMN_ADMIN_SIGNATURE_ID,
                table: TABLE_HOSPITAL_REGISTRATIONS);

            migrationBuilder.DropColumn(
                name: "contract_draft_file",
                table: TABLE_HOSPITAL_REGISTRATIONS);

            migrationBuilder.DropColumn(
                name: "contract_number",
                table: TABLE_HOSPITAL_REGISTRATIONS);

            migrationBuilder.DropColumn(
                name: "hospital_signature",
                table: TABLE_HOSPITAL_REGISTRATIONS);

            migrationBuilder.DropColumn(
                name: "signed_at",
                table: TABLE_HOSPITAL_REGISTRATIONS);
        }
    }
}
