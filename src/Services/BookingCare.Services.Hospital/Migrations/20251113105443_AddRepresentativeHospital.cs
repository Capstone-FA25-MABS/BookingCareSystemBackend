using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Hospital.Migrations
{
    /// <inheritdoc />
    public partial class AddRepresentativeHospital : Migration
    {
        private const string TableName = "hospital_registrations";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "phone",
                table: TableName,
                newName: "representative_phone");

            migrationBuilder.RenameColumn(
                name: "email",
                table: TableName,
                newName: "representative_email");

            migrationBuilder.RenameIndex(
                name: "IX_hospital_registrations_email",
                table: TableName,
                newName: "IX_hospital_registrations_representative_email");

            migrationBuilder.AddColumn<string>(
                name: "hospital_email",
                table: TableName,
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "hospital_phone",
                table: TableName,
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "representative_name",
                table: TableName,
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_hospital_registrations_hospital_email",
                table: TableName,
                column: "hospital_email");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_hospital_registrations_hospital_email",
                table: TableName);

            migrationBuilder.DropColumn(
                name: "hospital_email",
                table: TableName);

            migrationBuilder.DropColumn(
                name: "hospital_phone",
                table: TableName);

            migrationBuilder.DropColumn(
                name: "representative_name",
                table: TableName);

            migrationBuilder.RenameColumn(
                name: "representative_phone",
                table: TableName,
                newName: "phone");

            migrationBuilder.RenameColumn(
                name: "representative_email",
                table: TableName,
                newName: "email");

            migrationBuilder.RenameIndex(
                name: "IX_hospital_registrations_representative_email",
                table: TableName,
                newName: "IX_hospital_registrations_email");
        }
    }
}
