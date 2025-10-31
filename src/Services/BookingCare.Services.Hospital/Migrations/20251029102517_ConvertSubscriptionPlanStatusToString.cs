using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Hospital.Migrations
{
    /// <inheritdoc />
    public partial class ConvertSubscriptionPlanStatusToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop check constraint first (required before altering column)
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_subscription_plans_status')
                BEGIN
                    ALTER TABLE subscription_plans DROP CONSTRAINT CK_subscription_plans_status;
                END
            ");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "subscription_plans",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "ACTIVE",
                oldClrType: typeof(int),
                oldType: "int");

            // Recreate check constraint with string values
            migrationBuilder.AddCheckConstraint(
                name: "CK_subscription_plans_status",
                table: "subscription_plans",
                sql: "status IN ('ACTIVE', 'INACTIVE')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop check constraint first
            migrationBuilder.DropCheckConstraint(
                name: "CK_subscription_plans_status",
                table: "subscription_plans");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "subscription_plans",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "ACTIVE");

            // Recreate check constraint with int values (0 = ACTIVE, 1 = INACTIVE)
            migrationBuilder.AddCheckConstraint(
                name: "CK_subscription_plans_status",
                table: "subscription_plans",
                sql: "status IN (0, 1)");
        }
    }
}
