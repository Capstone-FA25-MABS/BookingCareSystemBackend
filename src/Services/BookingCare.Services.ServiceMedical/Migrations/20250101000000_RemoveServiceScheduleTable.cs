using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.ServiceMedical.Migrations
{
    /// <inheritdoc />
    public partial class RemoveServiceScheduleTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop foreign key constraint first
            migrationBuilder.DropForeignKey(
                name: "FK_service_schedules_services_service_id",
                table: "service_schedules");

            // Drop index
            migrationBuilder.DropIndex(
                name: "IX_service_schedules_service_id",
                table: "service_schedules");

            // Drop the table
            migrationBuilder.DropTable(
                name: "service_schedules");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recreate the table if needed to rollback
            migrationBuilder.CreateTable(
                name: "service_schedules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    service_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    pattern_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    hospital_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_schedules", x => x.id);
                    table.ForeignKey(
                        name: "FK_service_schedules_services_service_id",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_service_schedules_service_id",
                table: "service_schedules",
                column: "service_id");
        }
    }
}
