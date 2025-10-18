using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.ServiceMedical.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        private const string ServiceCategoriesTable = "service_categories";
        private const string ServicesTable = "services";
        private const string HospitalServicesTable = "hospital_services";
        private const string UniqueIdentifierType = "uniqueidentifier";
        private const string NvarcharMaxType = "nvarchar(max)";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: ServiceCategoriesTable,
                columns: table => new
                {
                    id = table.Column<Guid>(type: UniqueIdentifierType, nullable: false, defaultValueSql: "NEWID()"),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: NvarcharMaxType, nullable: true),
                    image_url = table.Column<string>(type: NvarcharMaxType, nullable: true),
                    parent_id = table.Column<Guid>(type: UniqueIdentifierType, nullable: true),
                    status = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "INACTIVE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_categories", x => x.id);
                    table.CheckConstraint("CK_ServiceCategory_Status", "status IN ('ACTIVE', 'INACTIVE')");
                    table.ForeignKey(
                        name: "FK_service_categories_service_categories_parent_id",
                        column: x => x.parent_id,
                        principalTable: ServiceCategoriesTable,
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: ServicesTable,
                columns: table => new
                {
                    id = table.Column<Guid>(type: UniqueIdentifierType, nullable: false, defaultValueSql: "NEWID()"),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: NvarcharMaxType, nullable: true),
                    price = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    image_url = table.Column<string>(type: NvarcharMaxType, nullable: true),
                    hospital_id = table.Column<Guid>(type: UniqueIdentifierType, nullable: false),
                    service_category_id = table.Column<Guid>(type: UniqueIdentifierType, nullable: true),
                    status = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "INACTIVE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_services", x => x.id);
                    table.CheckConstraint("CK_Service_Status", "status IN ('ACTIVE', 'INACTIVE')");
                    table.ForeignKey(
                        name: "FK_services_service_categories_service_category_id",
                        column: x => x.service_category_id,
                        principalTable: ServiceCategoriesTable,
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: HospitalServicesTable,
                columns: table => new
                {
                    id = table.Column<Guid>(type: UniqueIdentifierType, nullable: false, defaultValueSql: "NEWID()"),
                    hospital_id = table.Column<Guid>(type: UniqueIdentifierType, nullable: false),
                    service_id = table.Column<Guid>(type: UniqueIdentifierType, nullable: false),
                    price = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    duration_time = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "INACTIVE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hospital_services", x => x.id);
                    table.CheckConstraint("CK_HospitalService_Status", "status IN ('ACTIVE', 'INACTIVE')");
                    table.ForeignKey(
                        name: "FK_hospital_services_services_service_id",
                        column: x => x.service_id,
                        principalTable: ServicesTable,
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_hospital_services_service_id",
                table: "hospital_services",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_categories_parent_id",
                table: "service_categories",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_services_service_category_id",
                table: "services",
                column: "service_category_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "hospital_services");

            migrationBuilder.DropTable(
                name: "services");

            migrationBuilder.DropTable(
                name: "service_categories");
        }
    }
}
