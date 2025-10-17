using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Payment.Migrations
{
    /// <inheritdoc />
    public partial class AddImageUrlPaymentMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "image_url",
                table: "payment_methods",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "payment_methods",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "payment_methods",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "payment_methods",
                keyColumn: "id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "payment_methods",
                keyColumn: "id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "payment_methods",
                keyColumn: "id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "image_url",
                value: null);

            migrationBuilder.UpdateData(
                table: "payment_methods",
                keyColumn: "id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                column: "image_url",
                value: "https://yt3.googleusercontent.com/JM1m2wng0JQUgSg9ZSEvz7G4Rwo7pYb4QBYip4PAhvGRyf1D_YTbL2DdDjOy0qOXssJPdz2r7Q=s900-c-k-c0x00ffffff-no-rj");

            migrationBuilder.UpdateData(
                table: "payment_methods",
                keyColumn: "id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"),
                column: "image_url",
                value: "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTzyLwczXxezKsQjX4t5uvXGWDvlwwOwuX-1A&s");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "image_url",
                table: "payment_methods");
        }
    }
}
