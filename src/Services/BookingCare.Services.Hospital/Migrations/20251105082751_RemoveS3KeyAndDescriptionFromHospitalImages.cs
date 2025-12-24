using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Hospital.Migrations
{
    /// <inheritdoc />
    public partial class RemoveS3KeyAndDescriptionFromHospitalImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "description",
                table: "hospital_images");

            migrationBuilder.DropColumn(
                name: "s3_key",
                table: "hospital_images");

            migrationBuilder.CreateTable(
                name: "hospital_service_medicals",
                columns: table => new
                {
                    hospital_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    service_medical_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hospital_service_medicals", x => new { x.hospital_id, x.service_medical_id });
                    table.ForeignKey(
                        name: "FK_hospital_service_medicals_hospitals_hospital_id",
                        column: x => x.hospital_id,
                        principalTable: "hospitals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hospital_service_types",
                columns: table => new
                {
                    hospital_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    service_type_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hospital_service_types", x => new { x.hospital_id, x.service_type_id });
                    table.ForeignKey(
                        name: "FK_hospital_service_types_hospitals_hospital_id",
                        column: x => x.hospital_id,
                        principalTable: "hospitals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "hospital_service_medicals");

            migrationBuilder.DropTable(
                name: "hospital_service_types");

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "hospital_images",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "s3_key",
                table: "hospital_images",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }
    }
}
