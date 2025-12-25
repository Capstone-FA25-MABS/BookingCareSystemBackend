using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Schedule.Migrations
{
    /// <inheritdoc />
    public partial class AddExceptionApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "review_comments",
                table: "service_medical_schedule_exceptions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "reviewed_at",
                table: "service_medical_schedule_exceptions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "reviewed_by",
                table: "service_medical_schedule_exceptions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "service_medical_schedule_exceptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "review_comments",
                table: "doctor_schedule_exceptions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "reviewed_at",
                table: "doctor_schedule_exceptions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "reviewed_by",
                table: "doctor_schedule_exceptions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "doctor_schedule_exceptions",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "review_comments",
                table: "service_medical_schedule_exceptions");

            migrationBuilder.DropColumn(
                name: "reviewed_at",
                table: "service_medical_schedule_exceptions");

            migrationBuilder.DropColumn(
                name: "reviewed_by",
                table: "service_medical_schedule_exceptions");

            migrationBuilder.DropColumn(
                name: "status",
                table: "service_medical_schedule_exceptions");

            migrationBuilder.DropColumn(
                name: "review_comments",
                table: "doctor_schedule_exceptions");

            migrationBuilder.DropColumn(
                name: "reviewed_at",
                table: "doctor_schedule_exceptions");

            migrationBuilder.DropColumn(
                name: "reviewed_by",
                table: "doctor_schedule_exceptions");

            migrationBuilder.DropColumn(
                name: "status",
                table: "doctor_schedule_exceptions");
        }
    }
}
