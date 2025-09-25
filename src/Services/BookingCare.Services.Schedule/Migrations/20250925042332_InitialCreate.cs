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
                name: "appointment_times",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    start_time = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    end_time = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointment_times", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "clinic_exceptions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    clinic_id = table.Column<long>(type: "bigint", nullable: false),
                    exception_date = table.Column<DateOnly>(type: "date", nullable: false),
                    reason = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clinic_exceptions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "schedule_patterns",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "FULL_DAY"),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schedule_patterns", x => x.id);
                    table.CheckConstraint("CK_SchedulePattern_Name", "[name] IN ('FULL_DAY', 'MORNING_ONLY', 'AFTERNOON_ONLY', 'EVENING_ONLY')");
                });

            migrationBuilder.CreateTable(
                name: "doctor_schedule_exceptions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    doctor_id = table.Column<long>(type: "bigint", nullable: false),
                    exception_date = table.Column<DateOnly>(type: "date", nullable: false),
                    appointment_time_id = table.Column<long>(type: "bigint", nullable: true),
                    exception_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    is_available = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    reason = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_doctor_schedule_exceptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_doctor_schedule_exceptions_appointment_times_appointment_time_id",
                        column: x => x.appointment_time_id,
                        principalTable: "appointment_times",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "doctor_daily_schedules",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    doctor_id = table.Column<long>(type: "bigint", nullable: false),
                    schedule_date = table.Column<DateOnly>(type: "date", nullable: false),
                    pattern_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_doctor_daily_schedules", x => x.id);
                    table.ForeignKey(
                        name: "FK_doctor_daily_schedules_schedule_patterns_pattern_id",
                        column: x => x.pattern_id,
                        principalTable: "schedule_patterns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "schedule_pattern_slots",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    pattern_id = table.Column<long>(type: "bigint", nullable: false),
                    appointment_time_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schedule_pattern_slots", x => x.id);
                    table.ForeignKey(
                        name: "FK_schedule_pattern_slots_appointment_times_appointment_time_id",
                        column: x => x.appointment_time_id,
                        principalTable: "appointment_times",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_schedule_pattern_slots_schedule_patterns_pattern_id",
                        column: x => x.pattern_id,
                        principalTable: "schedule_patterns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_schedules",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    service_id = table.Column<long>(type: "bigint", nullable: false),
                    pattern_id = table.Column<long>(type: "bigint", nullable: false),
                    clinic_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_schedules", x => x.id);
                    table.ForeignKey(
                        name: "FK_service_schedules_schedule_patterns_pattern_id",
                        column: x => x.pattern_id,
                        principalTable: "schedule_patterns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_appointment_times_start_time_end_time",
                table: "appointment_times",
                columns: new[] { "start_time", "end_time" });

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
                name: "IX_doctor_daily_schedules_pattern_id",
                table: "doctor_daily_schedules",
                column: "pattern_id");

            migrationBuilder.CreateIndex(
                name: "IX_doctor_daily_schedules_schedule_date",
                table: "doctor_daily_schedules",
                column: "schedule_date");

            migrationBuilder.CreateIndex(
                name: "IX_doctor_schedule_exceptions_appointment_time_id",
                table: "doctor_schedule_exceptions",
                column: "appointment_time_id");

            migrationBuilder.CreateIndex(
                name: "IX_doctor_schedule_exceptions_doctor_id_exception_date",
                table: "doctor_schedule_exceptions",
                columns: new[] { "doctor_id", "exception_date" });

            migrationBuilder.CreateIndex(
                name: "IX_doctor_schedule_exceptions_exception_type",
                table: "doctor_schedule_exceptions",
                column: "exception_type");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_pattern_slots_appointment_time_id",
                table: "schedule_pattern_slots",
                column: "appointment_time_id");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_pattern_slots_pattern_id_appointment_time_id",
                table: "schedule_pattern_slots",
                columns: new[] { "pattern_id", "appointment_time_id" });

            migrationBuilder.CreateIndex(
                name: "IX_schedule_patterns_name",
                table: "schedule_patterns",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_service_schedules_clinic_id",
                table: "service_schedules",
                column: "clinic_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_schedules_pattern_id",
                table: "service_schedules",
                column: "pattern_id");

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
                name: "schedule_pattern_slots");

            migrationBuilder.DropTable(
                name: "service_schedules");

            migrationBuilder.DropTable(
                name: "appointment_times");

            migrationBuilder.DropTable(
                name: "schedule_patterns");
        }
    }
}
