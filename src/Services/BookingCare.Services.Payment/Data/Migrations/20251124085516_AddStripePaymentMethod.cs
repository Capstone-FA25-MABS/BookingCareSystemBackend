using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.Payment.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStripePaymentMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "payment_methods",
                columns: new[] { "id", "description", "image_url", "name", "status" },
                values: new object[] { new Guid("88888888-8888-8888-8888-888888888888"), "Thanh toán qua Stripe", "https://images.ctfassets.net/fzn2n1nzq965/HTTOloNPhisV9P4hlMPNA/cacf1bb88b9fc492dfad34378d844280/Stripe_icon_-_square.svg", "STRIPE", "ACTIVE" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "payment_methods",
                keyColumn: "id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"));
        }
    }
}
