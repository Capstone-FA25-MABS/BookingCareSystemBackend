using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Hospital.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIndentityInfoCardPrivate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "id_card_address",
                table: "hospital_registrations");

            migrationBuilder.DropColumn(
                name: "id_card_back_image_url",
                table: "hospital_registrations");

            migrationBuilder.DropColumn(
                name: "id_card_dob",
                table: "hospital_registrations");

            migrationBuilder.DropColumn(
                name: "id_card_front_image_url",
                table: "hospital_registrations");

            migrationBuilder.DropColumn(
                name: "id_card_name",
                table: "hospital_registrations");

            migrationBuilder.DropColumn(
                name: "id_card_number",
                table: "hospital_registrations");

            migrationBuilder.DropColumn(
                name: "selfie_image_url",
                table: "hospital_registrations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "id_card_address",
                table: "hospital_registrations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "id_card_back_image_url",
                table: "hospital_registrations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "id_card_dob",
                table: "hospital_registrations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "id_card_front_image_url",
                table: "hospital_registrations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "id_card_name",
                table: "hospital_registrations",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "id_card_number",
                table: "hospital_registrations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "selfie_image_url",
                table: "hospital_registrations",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
