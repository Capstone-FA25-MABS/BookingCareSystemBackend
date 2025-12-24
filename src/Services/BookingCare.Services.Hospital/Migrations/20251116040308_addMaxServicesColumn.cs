using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Hospital.Migrations
{
    /// <inheritdoc />
    public partial class addMaxServicesColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "max_services",
                table: "subscription_plans",
                type: "int",
                nullable: true,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "CK_subscription_plans_max_services",
                table: "subscription_plans",
                sql: "(max_services IS NULL OR max_services >= 0)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_subscription_plans_max_services",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "max_services",
                table: "subscription_plans");
        }
    }
}
