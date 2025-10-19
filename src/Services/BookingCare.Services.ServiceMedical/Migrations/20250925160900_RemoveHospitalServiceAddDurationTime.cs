using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.ServiceMedical.Migrations
{
    /// <inheritdoc />
    public partial class RemoveHospitalServiceAddDurationTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "hospital_services");

            migrationBuilder.AddColumn<int>(
                name: "duration_time",
                table: "services",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "duration_time",
                table: "services");

            migrationBuilder.CreateTable(
                name: "hospital_services",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    service_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    duration_time = table.Column<int>(type: "int", nullable: false),
                    hospital_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    price = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    status = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "INACTIVE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hospital_services", x => x.id);
                    table.CheckConstraint("CK_HospitalService_Status", "status IN ('ACTIVE', 'INACTIVE')");
                    table.ForeignKey(
                        name: "FK_hospital_services_services_service_id",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_hospital_services_service_id",
                table: "hospital_services",
                column: "service_id");
        }
    }
}
