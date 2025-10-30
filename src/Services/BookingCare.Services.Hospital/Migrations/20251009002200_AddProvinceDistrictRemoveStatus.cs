using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Hospital.Migrations
{
    /// <inheritdoc />
    public partial class AddProvinceDistrictRemoveStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check and drop constraint if exists
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_hospitals_status' AND parent_object_id = OBJECT_ID('hospitals'))
                BEGIN
                    ALTER TABLE [hospitals] DROP CONSTRAINT [CK_hospitals_status];
                END
            ");

            // Check and drop column if exists
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE name = 'status' AND object_id = OBJECT_ID('hospitals'))
                BEGIN
                    ALTER TABLE [hospitals] DROP COLUMN [status];
                END
            ");

            migrationBuilder.AddColumn<string>(
                name: "district_id",
                table: "hospitals",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "province_id",
                table: "hospitals",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "district_id",
                table: "hospitals");

            migrationBuilder.DropColumn(
                name: "province_id",
                table: "hospitals");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "hospitals",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "ACTIVE");

            migrationBuilder.AddCheckConstraint(
                name: "CK_hospitals_status",
                table: "hospitals",
                sql: "status IN ('ACTIVE', 'INACTIVE')");
        }
    }
}
