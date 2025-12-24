using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.AI.Migrations
{
    /// <inheritdoc />
    public partial class removeagegender : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompletedItemsJson",
                table: "WorkoutPlans",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFullyCompleted",
                table: "WorkoutPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "BMR",
                table: "NutritionProfiles",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCompletedDate",
                table: "NutritionProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StreakCount",
                table: "NutritionProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TDEE",
                table: "NutritionProfiles",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CompletedItemsJson",
                table: "MealPlans",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFullyCompleted",
                table: "MealPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedItemsJson",
                table: "WorkoutPlans");

            migrationBuilder.DropColumn(
                name: "IsFullyCompleted",
                table: "WorkoutPlans");

            migrationBuilder.DropColumn(
                name: "BMR",
                table: "NutritionProfiles");

            migrationBuilder.DropColumn(
                name: "LastCompletedDate",
                table: "NutritionProfiles");

            migrationBuilder.DropColumn(
                name: "StreakCount",
                table: "NutritionProfiles");

            migrationBuilder.DropColumn(
                name: "TDEE",
                table: "NutritionProfiles");

            migrationBuilder.DropColumn(
                name: "CompletedItemsJson",
                table: "MealPlans");

            migrationBuilder.DropColumn(
                name: "IsFullyCompleted",
                table: "MealPlans");
        }
    }
}
