using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Appointment.Migrations
{
    /// <inheritdoc />
    public partial class AddNewPropertiesCancel : Migration
    {
        private const string TableName = "Appointments";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRescheduled",
                table: TableName,
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RescheduleToken",
                table: TableName,
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RescheduleTokenExpiry",
                table: TableName,
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRescheduled",
                table: TableName);

            migrationBuilder.DropColumn(
                name: "RescheduleToken",
                table: TableName);

            migrationBuilder.DropColumn(
                name: "RescheduleTokenExpiry",
                table: TableName);
        }
    }
}
