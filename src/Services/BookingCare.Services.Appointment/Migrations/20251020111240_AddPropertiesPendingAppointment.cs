using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Appointment.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertiesPendingAppointment : Migration
    {
        private const string TableName = "Appointments";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PendingNewAppointmentDate",
                table: TableName,
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PendingNewAppointmentTimeId",
                table: TableName,
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PendingNewDoctorId",
                table: TableName,
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PendingNewAppointmentDate",
                table: TableName);

            migrationBuilder.DropColumn(
                name: "PendingNewAppointmentTimeId",
                table: TableName);

            migrationBuilder.DropColumn(
                name: "PendingNewDoctorId",
                table: TableName);
        }
    }
}
