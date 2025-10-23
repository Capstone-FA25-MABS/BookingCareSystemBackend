using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Appointment.Migrations
{
    /// <inheritdoc />
    public partial class AddNewPropertiesAssignDoctor : Migration
    {
        private const string TableName = "Appointments";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignedDoctorId",
                table: TableName,
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SoftReservedUntil",
                table: TableName,
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedDoctorId",
                table: TableName);

            migrationBuilder.DropColumn(
                name: "SoftReservedUntil",
                table: TableName);
        }
    }
}
