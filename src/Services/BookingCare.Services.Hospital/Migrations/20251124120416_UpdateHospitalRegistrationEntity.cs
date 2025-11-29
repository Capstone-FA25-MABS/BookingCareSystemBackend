using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Hospital.Migrations
{
    /// <inheritdoc />
    public partial class UpdateHospitalRegistrationEntity : Migration
    {
        // Constants for table names to avoid magic strings
        private const string TABLE_HOSPITAL_REGISTRATIONS = "hospital_registrations";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "contract_date",
                table: TABLE_HOSPITAL_REGISTRATIONS,
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "contract_effective_date",
                table: TABLE_HOSPITAL_REGISTRATIONS,
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "contract_expiry_date",
                table: TABLE_HOSPITAL_REGISTRATIONS,
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "contract_date",
                table: TABLE_HOSPITAL_REGISTRATIONS);

            migrationBuilder.DropColumn(
                name: "contract_effective_date",
                table: TABLE_HOSPITAL_REGISTRATIONS);

            migrationBuilder.DropColumn(
                name: "contract_expiry_date",
                table: TABLE_HOSPITAL_REGISTRATIONS);
        }
    }
}
