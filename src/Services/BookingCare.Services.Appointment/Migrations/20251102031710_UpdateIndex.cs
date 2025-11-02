using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Appointment.Migrations
{
    /// <inheritdoc />
    public partial class UpdateIndex : Migration
    {
        // Constants to avoid string literal duplication (SonarQube S1192)
        private const string TableName = "Appointments";
        private const string DoctorIndexName = "IX_Doctor_Date_Time_Unique";
        private const string PatientIndexName = "IX_Patient_Date_Time_Unique";
        private const string AppointmentDateColumn = "AppointmentDate";
        private const string AppointmentTimeIdColumn = "AppointmentTimeId";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: DoctorIndexName,
                table: TableName);

            migrationBuilder.DropIndex(
                name: PatientIndexName,
                table: TableName);

            migrationBuilder.CreateIndex(
                name: DoctorIndexName,
                table: TableName,
                columns: new[] { "DoctorId", AppointmentDateColumn, AppointmentTimeIdColumn },
                unique: true,
                filter: "[Status] IN ('PENDING', 'CONFIRMED')");

            migrationBuilder.CreateIndex(
                name: PatientIndexName,
                table: TableName,
                columns: new[] { "PatientId", AppointmentDateColumn, AppointmentTimeIdColumn },
                unique: true,
                filter: "[Status] IN ('PENDING', 'CONFIRMED')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: DoctorIndexName,
                table: TableName);

            migrationBuilder.DropIndex(
                name: PatientIndexName,
                table: TableName);

            migrationBuilder.CreateIndex(
                name: DoctorIndexName,
                table: TableName,
                columns: new[] { "DoctorId", AppointmentDateColumn, AppointmentTimeIdColumn, "Status" },
                unique: true,
                filter: "[DoctorId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: PatientIndexName,
                table: TableName,
                columns: new[] { "PatientId", AppointmentDateColumn, AppointmentTimeIdColumn },
                unique: true);
        }
    }
}
