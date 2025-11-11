using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Payment.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionFieldsToPayOSMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "current_hospital_subscription_id",
                table: "payos_payment_mappings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "hospital_id",
                table: "payos_payment_mappings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_subscription_upgrade",
                table: "payos_payment_mappings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "subscription_plan_id",
                table: "payos_payment_mappings",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "current_hospital_subscription_id",
                table: "payos_payment_mappings");

            migrationBuilder.DropColumn(
                name: "hospital_id",
                table: "payos_payment_mappings");

            migrationBuilder.DropColumn(
                name: "is_subscription_upgrade",
                table: "payos_payment_mappings");

            migrationBuilder.DropColumn(
                name: "subscription_plan_id",
                table: "payos_payment_mappings");
        }
    }
}
