using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Appointment.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
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
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                columns: new[] { "DoctorId", "AppointmentDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Doctor_Date_Time_Unique",
                table: "Appointments",
                columns: new[] { "DoctorId", "AppointmentDate", "AppointmentTimeId" },
                unique: true,
                filter: "[DoctorId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Patient_Date_Status",
                table: "Appointments",
                columns: new[] { "PatientId", "AppointmentDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Patient_Date_Time_Unique",
                table: "Appointments",
                columns: new[] { "PatientId", "AppointmentDate", "AppointmentTimeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Appointments");
        }
    }
}
