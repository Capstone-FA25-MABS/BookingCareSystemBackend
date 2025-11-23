using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Schedule.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceMedicalSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "service_medical_daily_schedules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    service_medical_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    schedule_date = table.Column<DateOnly>(type: "date", nullable: false),
                    schedule_patterns = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_medical_daily_schedules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "service_medical_schedule_exceptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    service_medical_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    exception_date = table.Column<DateOnly>(type: "date", nullable: false),
                    appointment_time = table.Column<int>(type: "int", nullable: true),
                    exception_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    is_available = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    reason = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_medical_schedule_exceptions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_service_medical_daily_schedules_schedule_date",
                table: "service_medical_daily_schedules",
                column: "schedule_date");

            migrationBuilder.CreateIndex(
                name: "IX_service_medical_daily_schedules_service_medical_id",
                table: "service_medical_daily_schedules",
                column: "service_medical_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_medical_daily_schedules_service_medical_id_schedule_date",
                table: "service_medical_daily_schedules",
                columns: new[] { "service_medical_id", "schedule_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_medical_schedule_exceptions_exception_type",
                table: "service_medical_schedule_exceptions",
                column: "exception_type");

            migrationBuilder.CreateIndex(
                name: "IX_service_medical_schedule_exceptions_service_medical_id_exception_date",
                table: "service_medical_schedule_exceptions",
                columns: new[] { "service_medical_id", "exception_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "service_medical_daily_schedules");

            migrationBuilder.DropTable(
                name: "service_medical_schedule_exceptions");
        }
    }
}
