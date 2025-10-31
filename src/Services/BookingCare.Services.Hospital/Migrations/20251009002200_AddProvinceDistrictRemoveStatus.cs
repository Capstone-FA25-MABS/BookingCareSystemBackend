using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Hospital.Migrations
{
    /// <inheritdoc />
    public partial class AddProvinceDistrictRemoveStatus : Migration
    {
        private const string HospitalsTable = "hospitals";
        private const string StatusConstraint = "CK_hospitals_status";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check and drop constraint if exists
            migrationBuilder.Sql($@"
                IF EXISTS (SELECT * FROM sys.check_constraints WHERE name = '{StatusConstraint}' AND parent_object_id = OBJECT_ID('{HospitalsTable}'))
                BEGIN
                    ALTER TABLE [{HospitalsTable}] DROP CONSTRAINT [{StatusConstraint}];
                END
            ");

            // Check and drop column if exists
            migrationBuilder.Sql($@"
                IF EXISTS (SELECT * FROM sys.columns WHERE name = 'status' AND object_id = OBJECT_ID('{HospitalsTable}'))
                BEGIN
                    ALTER TABLE [{HospitalsTable}] DROP COLUMN [status];
                END
            ");

            migrationBuilder.AddColumn<string>(
                name: "district_id",
                table: HospitalsTable,
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "province_id",
                table: HospitalsTable,
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "district_id",
                table: HospitalsTable);

            migrationBuilder.DropColumn(
                name: "province_id",
                table: HospitalsTable);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: HospitalsTable,
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "ACTIVE");

            migrationBuilder.AddCheckConstraint(
                name: StatusConstraint,
                table: HospitalsTable,
                sql: "status IN ('ACTIVE', 'INACTIVE')");
        }
    }
}
