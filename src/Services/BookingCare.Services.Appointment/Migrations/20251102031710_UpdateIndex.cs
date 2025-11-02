using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Appointment.Migrations
{
    /// <inheritdoc />
    public partial class UpdateIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Doctor_Date_Time_Unique",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Patient_Date_Time_Unique",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Doctor_Date_Time_Unique",
                table: "Appointments",
                columns: new[] { "DoctorId", "AppointmentDate", "AppointmentTimeId" },
                unique: true,
                filter: "[Status] IN ('PENDING', 'CONFIRMED')");

            migrationBuilder.CreateIndex(
                name: "IX_Patient_Date_Time_Unique",
                table: "Appointments",
                columns: new[] { "PatientId", "AppointmentDate", "AppointmentTimeId" },
                unique: true,
                filter: "[Status] IN ('PENDING', 'CONFIRMED')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Doctor_Date_Time_Unique",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Patient_Date_Time_Unique",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Doctor_Date_Time_Unique",
                table: "Appointments",
                columns: new[] { "DoctorId", "AppointmentDate", "AppointmentTimeId", "Status" },
                unique: true,
                filter: "[DoctorId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Patient_Date_Time_Unique",
                table: "Appointments",
                columns: new[] { "PatientId", "AppointmentDate", "AppointmentTimeId" },
                unique: true);
        }
    }
}
