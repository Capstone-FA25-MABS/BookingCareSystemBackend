using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Hospital.Migrations
{
    /// <inheritdoc />
    public partial class AddRegisterHospital : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Only create hospital_registrations table
            // Don't touch status columns, district_id, province_id
            migrationBuilder.CreateTable(
                name: "hospital_registrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    hospital_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    license_file = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    business_certificate_file = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    identity_card_file = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    tax_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    contract_file = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    hospital_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hospital_registrations", x => x.id);
                    table.ForeignKey(
                        name: "FK_hospital_registrations_hospitals_hospital_id",
                        column: x => x.hospital_id,
                        principalTable: "hospitals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_hospital_registrations_email",
                table: "hospital_registrations",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "IX_hospital_registrations_hospital_id",
                table: "hospital_registrations",
                column: "hospital_id");

            migrationBuilder.CreateIndex(
                name: "IX_hospital_registrations_tax_code",
                table: "hospital_registrations",
                column: "tax_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Only drop hospital_registrations table
            migrationBuilder.DropTable(
                name: "hospital_registrations");
        }
    }
}
