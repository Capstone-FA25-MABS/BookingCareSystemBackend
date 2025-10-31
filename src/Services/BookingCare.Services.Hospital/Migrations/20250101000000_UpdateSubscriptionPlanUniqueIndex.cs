using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Hospital.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSubscriptionPlanUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the old unique index on name only
            migrationBuilder.DropIndex(
                name: "IX_subscription_plans_name",
                table: "subscription_plans");

            // Create new unique index on (name, billing_cycle)
            migrationBuilder.CreateIndex(
                name: "IX_subscription_plans_name_billing_cycle_unique",
                table: "subscription_plans",
                columns: new[] { "name", "billing_cycle" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the composite unique index
            migrationBuilder.DropIndex(
                name: "IX_subscription_plans_name_billing_cycle_unique",
                table: "subscription_plans");

            // Restore the old unique index on name only
            migrationBuilder.CreateIndex(
                name: "IX_subscription_plans_name",
                table: "subscription_plans",
                column: "name",
                unique: true);
        }
    }
}

