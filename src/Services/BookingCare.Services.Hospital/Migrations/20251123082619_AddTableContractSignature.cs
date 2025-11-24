using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Hospital.Migrations
{
    /// <inheritdoc />
    public partial class AddTableContractSignature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "admin_signature_id",
                table: "hospital_registrations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "contract_draft_file",
                table: "hospital_registrations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "contract_number",
                table: "hospital_registrations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "hospital_signature",
                table: "hospital_registrations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "signed_at",
                table: "hospital_registrations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "admin_signatures",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    admin_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    full_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    position = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    signature_image_url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_signatures", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "contract_signing_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    registration_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    token = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_used = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    used_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    signed_from_ip = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contract_signing_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_contract_signing_tokens_hospital_registrations_registration_id",
                        column: x => x.registration_id,
                        principalTable: "hospital_registrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_hospital_registrations_admin_signature_id",
                table: "hospital_registrations",
                column: "admin_signature_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_signatures_admin_id",
                table: "admin_signatures",
                column: "admin_id");

            migrationBuilder.CreateIndex(
                name: "IX_contract_signing_tokens_registration_id",
                table: "contract_signing_tokens",
                column: "registration_id");

            migrationBuilder.CreateIndex(
                name: "IX_contract_signing_tokens_token",
                table: "contract_signing_tokens",
                column: "token",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_hospital_registrations_admin_signatures_admin_signature_id",
                table: "hospital_registrations",
                column: "admin_signature_id",
                principalTable: "admin_signatures",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_hospital_registrations_admin_signatures_admin_signature_id",
                table: "hospital_registrations");

            migrationBuilder.DropTable(
                name: "admin_signatures");

            migrationBuilder.DropTable(
                name: "contract_signing_tokens");

            migrationBuilder.DropIndex(
                name: "IX_hospital_registrations_admin_signature_id",
                table: "hospital_registrations");

            migrationBuilder.DropColumn(
                name: "admin_signature_id",
                table: "hospital_registrations");

            migrationBuilder.DropColumn(
                name: "contract_draft_file",
                table: "hospital_registrations");

            migrationBuilder.DropColumn(
                name: "contract_number",
                table: "hospital_registrations");

            migrationBuilder.DropColumn(
                name: "hospital_signature",
                table: "hospital_registrations");

            migrationBuilder.DropColumn(
                name: "signed_at",
                table: "hospital_registrations");
        }
    }
}
