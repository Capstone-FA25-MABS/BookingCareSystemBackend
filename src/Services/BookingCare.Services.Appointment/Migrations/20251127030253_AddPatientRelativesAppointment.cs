using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Appointment.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientRelativesAppointment : Migration
    {
        private const string APPOINTMENTS_TABLE = "Appointments";
        private const string IX_PATIENT_DATE_TIME_UNIQUE = "IX_Patient_Date_Time_Unique";
        private const string IX_RELATIVE_DATE_TIME_UNIQUE = "IX_Relative_Date_Time_Unique";
        private const string PATIENT_DATE_TIME_FILTER = "[RelativeId] IS NULL AND [Status] IN ('PENDING', 'CONFIRMED')";
        private const string RELATIVE_DATE_TIME_FILTER = "[RelativeId] IS NOT NULL AND [Status] IN ('PENDING', 'CONFIRMED')";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: IX_PATIENT_DATE_TIME_UNIQUE,
                table: APPOINTMENTS_TABLE);

            migrationBuilder.AddColumn<Guid>(
                name: "RelativeId",
                table: APPOINTMENTS_TABLE,
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: IX_PATIENT_DATE_TIME_UNIQUE,
                table: APPOINTMENTS_TABLE,
                columns: new[] { "PatientId", "AppointmentDate", "AppointmentTimeId" },
                unique: true,
                filter: PATIENT_DATE_TIME_FILTER);

            migrationBuilder.CreateIndex(
                name: IX_RELATIVE_DATE_TIME_UNIQUE,
                table: APPOINTMENTS_TABLE,
                columns: new[] { "RelativeId", "AppointmentDate", "AppointmentTimeId" },
                unique: true,
                filter: RELATIVE_DATE_TIME_FILTER);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: IX_PATIENT_DATE_TIME_UNIQUE,
                table: APPOINTMENTS_TABLE);

            migrationBuilder.DropIndex(
                name: IX_RELATIVE_DATE_TIME_UNIQUE,
                table: APPOINTMENTS_TABLE);

            migrationBuilder.DropColumn(
                name: "RelativeId",
                table: APPOINTMENTS_TABLE);

            migrationBuilder.CreateIndex(
                name: IX_PATIENT_DATE_TIME_UNIQUE,
                table: APPOINTMENTS_TABLE,
                columns: new[] { "PatientId", "AppointmentDate", "AppointmentTimeId" },
                unique: true,
                filter: "[Status] IN ('PENDING', 'CONFIRMED')");
        }
    }
}
