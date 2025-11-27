using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Appointment.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Doctor_Date_Status",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Doctor_Date_Time_Unique",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Doctor_Date_Status",
                table: "Appointments",
                columns: new[] { "DoctorId", "AppointmentDate", "Status" },
                filter: "[DoctorId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Doctor_Date_Time_Unique",
                table: "Appointments",
                columns: new[] { "DoctorId", "AppointmentDate", "AppointmentTimeId" },
                unique: true,
                filter: "[DoctorId] IS NOT NULL AND [Status] IN ('PENDING', 'CONFIRMED')");

            migrationBuilder.CreateIndex(
                name: "IX_Service_Date_Status",
                table: "Appointments",
                columns: new[] { "ServiceId", "AppointmentDate", "Status" },
                filter: "[ServiceId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Service_Date_Time",
                table: "Appointments",
                columns: new[] { "ServiceId", "AppointmentDate", "AppointmentTimeId" },
                filter: "[ServiceId] IS NOT NULL AND [Status] IN ('PENDING', 'CONFIRMED')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Doctor_Date_Status",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Doctor_Date_Time_Unique",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Service_Date_Status",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Service_Date_Time",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Doctor_Date_Status",
                table: "Appointments",
                columns: new[] { "DoctorId", "AppointmentDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Doctor_Date_Time_Unique",
                table: "Appointments",
                columns: new[] { "DoctorId", "AppointmentDate", "AppointmentTimeId" },
                unique: true,
                filter: "[Status] IN ('PENDING', 'CONFIRMED')");
        }
    }
}
