using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Schedule.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "clinic_exceptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    clinic_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    exception_date = table.Column<DateOnly>(type: "date", nullable: false),
                    reason = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clinic_exceptions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "doctor_daily_schedules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    doctor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    schedule_date = table.Column<DateOnly>(type: "date", nullable: false),
                    schedule_patterns = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_doctor_daily_schedules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "doctor_schedule_exceptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    doctor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    exception_date = table.Column<DateOnly>(type: "date", nullable: false),
                    appointment_time = table.Column<int>(type: "int", nullable: true),
                    exception_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    is_available = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    reason = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_doctor_schedule_exceptions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "service_schedules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    service_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    schedule_patterns = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    clinic_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_schedules", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_clinic_exceptions_clinic_id_exception_date",
                table: "clinic_exceptions",
                columns: new[] { "clinic_id", "exception_date" });

            migrationBuilder.CreateIndex(
                name: "IX_doctor_daily_schedules_doctor_id",
                table: "doctor_daily_schedules",
                column: "doctor_id");

            migrationBuilder.CreateIndex(
                name: "IX_doctor_daily_schedules_doctor_id_schedule_date",
                table: "doctor_daily_schedules",
                columns: new[] { "doctor_id", "schedule_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_doctor_daily_schedules_schedule_date",
                table: "doctor_daily_schedules",
                column: "schedule_date");

            migrationBuilder.CreateIndex(
                name: "IX_doctor_schedule_exceptions_doctor_id_exception_date",
                table: "doctor_schedule_exceptions",
                columns: new[] { "doctor_id", "exception_date" });

            migrationBuilder.CreateIndex(
                name: "IX_doctor_schedule_exceptions_exception_type",
                table: "doctor_schedule_exceptions",
                column: "exception_type");

            migrationBuilder.CreateIndex(
                name: "IX_service_schedules_clinic_id",
                table: "service_schedules",
                column: "clinic_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_schedules_service_id",
                table: "service_schedules",
                column: "service_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "clinic_exceptions");

            migrationBuilder.DropTable(
                name: "doctor_daily_schedules");

            migrationBuilder.DropTable(
                name: "doctor_schedule_exceptions");

            migrationBuilder.DropTable(
                name: "service_schedules");
        }
    }
}
