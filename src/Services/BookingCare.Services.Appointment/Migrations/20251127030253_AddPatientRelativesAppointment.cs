using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Appointment.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientRelativesAppointment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Patient_Date_Time_Unique",
                table: "Appointments");

            migrationBuilder.AddColumn<Guid>(
                name: "RelativeId",
                table: "Appointments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patient_Date_Time_Unique",
                table: "Appointments",
                columns: new[] { "PatientId", "AppointmentDate", "AppointmentTimeId" },
                unique: true,
                filter: "[RelativeId] IS NULL AND [Status] IN ('PENDING', 'CONFIRMED')");

            migrationBuilder.CreateIndex(
                name: "IX_Relative_Date_Time_Unique",
                table: "Appointments",
                columns: new[] { "RelativeId", "AppointmentDate", "AppointmentTimeId" },
                unique: true,
                filter: "[RelativeId] IS NOT NULL AND [Status] IN ('PENDING', 'CONFIRMED')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Patient_Date_Time_Unique",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Relative_Date_Time_Unique",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "RelativeId",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Patient_Date_Time_Unique",
                table: "Appointments",
                columns: new[] { "PatientId", "AppointmentDate", "AppointmentTimeId" },
                unique: true,
                filter: "[Status] IN ('PENDING', 'CONFIRMED')");
        }
    }
}
