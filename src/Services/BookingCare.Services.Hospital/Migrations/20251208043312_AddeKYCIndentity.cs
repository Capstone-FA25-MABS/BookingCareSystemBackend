using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Hospital.Migrations
{
    /// <inheritdoc />
    public partial class AddeKYCIndentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ekyc_session_id",
                table: "hospital_registrations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ekyc_status",
                table: "hospital_registrations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ekyc_verified_at",
                table: "hospital_registrations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "face_match_score",
                table: "hospital_registrations",
                type: "decimal(18,2)",
                nullable: true);

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

            migrationBuilder.AddColumn<decimal>(
                name: "liveness_score",
                table: "hospital_registrations",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "selfie_image_url",
                table: "hospital_registrations",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ekyc_session_id",
                table: "hospital_registrations");

            migrationBuilder.DropColumn(
                name: "ekyc_status",
                table: "hospital_registrations");

            migrationBuilder.DropColumn(
                name: "ekyc_verified_at",
                table: "hospital_registrations");

            migrationBuilder.DropColumn(
                name: "face_match_score",
                table: "hospital_registrations");

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
                name: "liveness_score",
                table: "hospital_registrations");

            migrationBuilder.DropColumn(
                name: "selfie_image_url",
                table: "hospital_registrations");
        }
    }
}
