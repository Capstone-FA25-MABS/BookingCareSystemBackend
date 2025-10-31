using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Hospital.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionPlanFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "hospitals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    account_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    background_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    avatar_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hospitals", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "subscription_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    price = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    billing_cycle = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "MONTHLY"),
                    max_doctors = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    max_specialties = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    features = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    auto_renew = table.Column<bool>(type: "bit", nullable: false),
                    max_appointments = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_plans", x => x.id);
                    table.CheckConstraint("CK_subscription_plans_billing_cycle", "billing_cycle IN ('MONTHLY', 'QUARTERLY', 'YEARLY')");
                    table.CheckConstraint("CK_subscription_plans_max_doctors", "max_doctors >= 0");
                    table.CheckConstraint("CK_subscription_plans_max_specialties", "max_specialties >= 0");
                    table.CheckConstraint("CK_subscription_plans_price", "price >= 0");
                    table.CheckConstraint("CK_subscription_plans_status", "status IN ('ACTIVE', 'INACTIVE')");
                });

            migrationBuilder.CreateTable(
                name: "hospital_images",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    hospital_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    s3_key = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    image_url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hospital_images", x => x.id);
                    table.ForeignKey(
                        name: "FK_hospital_images_hospitals_hospital_id",
                        column: x => x.hospital_id,
                        principalTable: "hospitals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hospital_specialties",
                columns: table => new
                {
                    hospital_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    specialty_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hospital_specialties", x => new { x.hospital_id, x.specialty_id });
                    table.ForeignKey(
                        name: "FK_hospital_specialties_hospitals_hospital_id",
                        column: x => x.hospital_id,
                        principalTable: "hospitals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hospital_subscriptions",
                columns: table => new
                {
                    hospital_subscription_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    hospital_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    subscription_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    start_date = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    end_date = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hospital_subscriptions", x => x.hospital_subscription_id);
                    table.CheckConstraint("CK_hospital_subscriptions_end_date", "end_date > start_date");
                    table.CheckConstraint("CK_hospital_subscriptions_status", "status IN ('ACTIVE', 'EXPIRED', 'CANCELLED', 'PENDING', 'TRIAL')");
                    table.ForeignKey(
                        name: "FK_hospital_subscriptions_hospitals_hospital_id",
                        column: x => x.hospital_id,
                        principalTable: "hospitals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_hospital_subscriptions_subscription_plans_subscription_id",
                        column: x => x.subscription_id,
                        principalTable: "subscription_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_hospital_images_hospital_id",
                table: "hospital_images",
                column: "hospital_id");

            migrationBuilder.CreateIndex(
                name: "IX_hospital_subscriptions_hospital_active_unique",
                table: "hospital_subscriptions",
                columns: new[] { "hospital_id", "status" },
                filter: "status IN ('ACTIVE', 'TRIAL')");

            migrationBuilder.CreateIndex(
                name: "IX_hospital_subscriptions_subscription_id",
                table: "hospital_subscriptions",
                column: "subscription_id");

            migrationBuilder.CreateIndex(
                name: "IX_hospitals_email",
                table: "hospitals",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_plans_name",
                table: "subscription_plans",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "hospital_images");

            migrationBuilder.DropTable(
                name: "hospital_specialties");

            migrationBuilder.DropTable(
                name: "hospital_subscriptions");

            migrationBuilder.DropTable(
                name: "hospitals");

            migrationBuilder.DropTable(
                name: "subscription_plans");
        }
    }
}
