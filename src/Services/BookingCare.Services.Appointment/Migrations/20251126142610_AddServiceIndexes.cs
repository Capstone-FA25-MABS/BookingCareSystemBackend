using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Appointment.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceIndexes : Migration
    {
        private const string APPOINTMENTS_TABLE = "Appointments";
        private const string IX_DOCTOR_DATE_STATUS = "IX_Doctor_Date_Status";
        private const string IX_DOCTOR_DATE_TIME_UNIQUE = "IX_Doctor_Date_Time_Unique";
        private const string IX_SERVICE_DATE_STATUS = "IX_Service_Date_Status";
        private const string IX_SERVICE_DATE_TIME = "IX_Service_Date_Time";
        private const string DOCTOR_ID_COLUMN = "DoctorId";
        private const string SERVICE_ID_COLUMN = "ServiceId";
        private const string APPOINTMENT_DATE_COLUMN = "AppointmentDate";
        private const string APPOINTMENT_TIME_ID_COLUMN = "AppointmentTimeId";
        private const string STATUS_COLUMN = "Status";
        private const string DOCTOR_DATE_STATUS_FILTER = "[DoctorId] IS NOT NULL";
        private const string DOCTOR_DATE_TIME_UNIQUE_FILTER = "[DoctorId] IS NOT NULL AND [Status] IN ('PENDING', 'CONFIRMED')";
        private const string SERVICE_DATE_STATUS_FILTER = "[ServiceId] IS NOT NULL";
        private const string SERVICE_DATE_TIME_FILTER = "[ServiceId] IS NOT NULL AND [Status] IN ('PENDING', 'CONFIRMED')";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: IX_DOCTOR_DATE_STATUS,
                table: APPOINTMENTS_TABLE);

            migrationBuilder.DropIndex(
                name: IX_DOCTOR_DATE_TIME_UNIQUE,
                table: APPOINTMENTS_TABLE);

            migrationBuilder.CreateIndex(
                name: IX_DOCTOR_DATE_STATUS,
                table: APPOINTMENTS_TABLE,
                columns: new[] { DOCTOR_ID_COLUMN, APPOINTMENT_DATE_COLUMN, STATUS_COLUMN },
                filter: DOCTOR_DATE_STATUS_FILTER);

            migrationBuilder.CreateIndex(
                name: IX_DOCTOR_DATE_TIME_UNIQUE,
                table: APPOINTMENTS_TABLE,
                columns: new[] { DOCTOR_ID_COLUMN, APPOINTMENT_DATE_COLUMN, APPOINTMENT_TIME_ID_COLUMN },
                unique: true,
                filter: DOCTOR_DATE_TIME_UNIQUE_FILTER);

            migrationBuilder.CreateIndex(
                name: IX_SERVICE_DATE_STATUS,
                table: APPOINTMENTS_TABLE,
                columns: new[] { SERVICE_ID_COLUMN, APPOINTMENT_DATE_COLUMN, STATUS_COLUMN },
                filter: SERVICE_DATE_STATUS_FILTER);

            migrationBuilder.CreateIndex(
                name: IX_SERVICE_DATE_TIME,
                table: APPOINTMENTS_TABLE,
                columns: new[] { SERVICE_ID_COLUMN, APPOINTMENT_DATE_COLUMN, APPOINTMENT_TIME_ID_COLUMN },
                filter: SERVICE_DATE_TIME_FILTER);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: IX_DOCTOR_DATE_STATUS,
                table: APPOINTMENTS_TABLE);

            migrationBuilder.DropIndex(
                name: IX_DOCTOR_DATE_TIME_UNIQUE,
                table: APPOINTMENTS_TABLE);

            migrationBuilder.DropIndex(
                name: IX_SERVICE_DATE_STATUS,
                table: APPOINTMENTS_TABLE);

            migrationBuilder.DropIndex(
                name: IX_SERVICE_DATE_TIME,
                table: APPOINTMENTS_TABLE);

            migrationBuilder.CreateIndex(
                name: IX_DOCTOR_DATE_STATUS,
                table: APPOINTMENTS_TABLE,
                columns: new[] { DOCTOR_ID_COLUMN, APPOINTMENT_DATE_COLUMN, STATUS_COLUMN });

            migrationBuilder.CreateIndex(
                name: IX_DOCTOR_DATE_TIME_UNIQUE,
                table: APPOINTMENTS_TABLE,
                columns: new[] { DOCTOR_ID_COLUMN, APPOINTMENT_DATE_COLUMN, APPOINTMENT_TIME_ID_COLUMN },
                unique: true,
                filter: "[Status] IN ('PENDING', 'CONFIRMED')");
        }
    }
}
