using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Hospital.Migrations
{
    /// <inheritdoc />
    public partial class AddUsageCountsToHospitalSubscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "appointment_count",
                table: "hospital_subscriptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "doctor_count",
                table: "hospital_subscriptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "service_count",
                table: "hospital_subscriptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "specialty_count",
                table: "hospital_subscriptions",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "appointment_count",
                table: "hospital_subscriptions");

            migrationBuilder.DropColumn(
                name: "doctor_count",
                table: "hospital_subscriptions");

            migrationBuilder.DropColumn(
                name: "service_count",
                table: "hospital_subscriptions");

            migrationBuilder.DropColumn(
                name: "specialty_count",
                table: "hospital_subscriptions");
        }
    }
}
