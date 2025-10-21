using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Appointment.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertiesPendingAppointment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PendingNewAppointmentDate",
                table: "Appointments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PendingNewAppointmentTimeId",
                table: "Appointments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PendingNewDoctorId",
                table: "Appointments",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PendingNewAppointmentDate",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "PendingNewAppointmentTimeId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "PendingNewDoctorId",
                table: "Appointments");
        }
    }
}
