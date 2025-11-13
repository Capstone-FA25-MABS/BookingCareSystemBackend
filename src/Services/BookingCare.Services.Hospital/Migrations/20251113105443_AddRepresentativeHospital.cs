using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Hospital.Migrations
{
    /// <inheritdoc />
    public partial class AddRepresentativeHospital : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "phone",
                table: "hospital_registrations",
                newName: "representative_phone");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "hospital_registrations",
                newName: "representative_email");

            migrationBuilder.RenameIndex(
                name: "IX_hospital_registrations_email",
                table: "hospital_registrations",
                newName: "IX_hospital_registrations_representative_email");

            migrationBuilder.AddColumn<string>(
                name: "hospital_email",
                table: "hospital_registrations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "hospital_phone",
                table: "hospital_registrations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "representative_name",
                table: "hospital_registrations",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_hospital_registrations_hospital_email",
                table: "hospital_registrations",
                column: "hospital_email");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_hospital_registrations_hospital_email",
                table: "hospital_registrations");

            migrationBuilder.DropColumn(
                name: "hospital_email",
                table: "hospital_registrations");

            migrationBuilder.DropColumn(
                name: "hospital_phone",
                table: "hospital_registrations");

            migrationBuilder.DropColumn(
                name: "representative_name",
                table: "hospital_registrations");

            migrationBuilder.RenameColumn(
                name: "representative_phone",
                table: "hospital_registrations",
                newName: "phone");

            migrationBuilder.RenameColumn(
                name: "representative_email",
                table: "hospital_registrations",
                newName: "email");

            migrationBuilder.RenameIndex(
                name: "IX_hospital_registrations_representative_email",
                table: "hospital_registrations",
                newName: "IX_hospital_registrations_email");
        }
    }
}
