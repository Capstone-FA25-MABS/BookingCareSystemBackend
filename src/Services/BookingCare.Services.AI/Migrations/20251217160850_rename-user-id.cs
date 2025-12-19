using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.AI.Migrations
{
    /// <inheritdoc />
    public partial class renameuserid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "WorkoutPlans",
                newName: "AccountId");

            migrationBuilder.RenameIndex(
                name: "IX_WorkoutPlans_UserId_Date",
                table: "WorkoutPlans",
                newName: "IX_WorkoutPlans_AccountId_Date");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "NutritionProfiles",
                newName: "AccountId");

            migrationBuilder.RenameIndex(
                name: "IX_NutritionProfiles_UserId",
                table: "NutritionProfiles",
                newName: "IX_NutritionProfiles_AccountId");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "MealPlans",
                newName: "AccountId");

            migrationBuilder.RenameIndex(
                name: "IX_MealPlans_UserId_Date",
                table: "MealPlans",
                newName: "IX_MealPlans_AccountId_Date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AccountId",
                table: "WorkoutPlans",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_WorkoutPlans_AccountId_Date",
                table: "WorkoutPlans",
                newName: "IX_WorkoutPlans_UserId_Date");

            migrationBuilder.RenameColumn(
                name: "AccountId",
                table: "NutritionProfiles",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_NutritionProfiles_AccountId",
                table: "NutritionProfiles",
                newName: "IX_NutritionProfiles_UserId");

            migrationBuilder.RenameColumn(
                name: "AccountId",
                table: "MealPlans",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_MealPlans_AccountId_Date",
                table: "MealPlans",
                newName: "IX_MealPlans_UserId_Date");
        }
    }
}
