using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.AI.Migrations
{
    /// <inheritdoc />
    public partial class AddReasonsJsonToDermatologyCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReasonsJson",
                table: "DermatologyDiseaseCache",
                type: "nvarchar(max)",
                nullable: true);

            // Clean up existing data: remove </think> tags from VietnameseName
            migrationBuilder.Sql(@"
            UPDATE DermatologyDiseaseCache
            SET VietnameseName = LTRIM(RTRIM(REPLACE(
                REPLACE(
                    REPLACE(VietnameseName, '</think>', ''),
                    '<think>', ''
                ),
                CHAR(13) + CHAR(10), ' '
            )))
            WHERE VietnameseName LIKE '%</think>%' 
               OR VietnameseName LIKE '%<think>%';
        ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReasonsJson",
                table: "DermatologyDiseaseCache");
        }
    }
}
