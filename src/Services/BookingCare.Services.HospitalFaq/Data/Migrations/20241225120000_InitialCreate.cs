using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.HospitalFaq.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        private const string TABLE_HOSPITAL_FAQS = "hospital_faqs";
        private const string TYPE_UNIQUEIDENTIFIER = "uniqueidentifier";
        private const string TYPE_DATETIME2 = "datetime2";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: TABLE_HOSPITAL_FAQS,
                columns: table => new
                {
                    id = table.Column<Guid>(type: TYPE_UNIQUEIDENTIFIER, nullable: false),
                    hospital_id = table.Column<Guid>(type: TYPE_UNIQUEIDENTIFIER, nullable: false),
                    question = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    answer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_by = table.Column<Guid>(type: TYPE_UNIQUEIDENTIFIER, nullable: false),
                    display_order = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: TYPE_DATETIME2, nullable: false, defaultValueSql: "GETDATE()"),
                    updated_at = table.Column<DateTime>(type: TYPE_DATETIME2, nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hospital_faqs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_hospital_faqs_hospital_id",
                table: TABLE_HOSPITAL_FAQS,
                column: "hospital_id");

            migrationBuilder.CreateIndex(
                name: "IX_hospital_faqs_hospital_id_display_order",
                table: TABLE_HOSPITAL_FAQS,
                columns: new[] { "hospital_id", "display_order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: TABLE_HOSPITAL_FAQS);
        }
    }
}

