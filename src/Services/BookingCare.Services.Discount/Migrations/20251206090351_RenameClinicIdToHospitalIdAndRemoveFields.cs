using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Discount.Migrations
{
    /// <inheritdoc />
    public partial class RenameClinicIdToHospitalIdAndRemoveFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "doctor_id",
                table: "discounts");

            migrationBuilder.DropColumn(
                name: "specialty_id",
                table: "discounts");

            migrationBuilder.RenameColumn(
                name: "clinic_id",
                table: "discounts",
                newName: "hospital_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "hospital_id",
                table: "discounts",
                newName: "clinic_id");

            migrationBuilder.AddColumn<Guid>(
                name: "doctor_id",
                table: "discounts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "specialty_id",
                table: "discounts",
                type: "uniqueidentifier",
                nullable: true);
        }
    }
}
