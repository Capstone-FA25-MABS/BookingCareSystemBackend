using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Appointment.Migrations
{
    /// <inheritdoc />
    public partial class initdatabaseappointment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Appointments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelativeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SpecialtyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AppointmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AppointmentTimeId = table.Column<int>(type: "int", nullable: false),
                    HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AppointmentType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Result = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Symptoms = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    AttachmentUrls = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CancelledBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsRescheduled = table.Column<bool>(type: "bit", nullable: false),
                    RescheduleToken = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RescheduleTokenExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PendingRescheduleAction = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AssignedDoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SoftReservedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PendingNewDoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PendingNewAppointmentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PendingNewAppointmentTimeId = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                });

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
                name: "IX_Patient_Date_Status",
                table: "Appointments",
                columns: new[] { "PatientId", "AppointmentDate", "Status" });

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
            migrationBuilder.DropTable(
                name: "Appointments");
        }
    }
}
