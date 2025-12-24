using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingCare.Services.AI.Migrations
{
    /// <inheritdoc />
    public partial class PersonalizedNutritionRecommendations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NutritionProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HeightCm = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WeightKg = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BMI = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ActivityLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    HealthGoal = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TargetCalories = table.Column<int>(type: "int", nullable: false),
                    TargetProteinG = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TargetCarbsG = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TargetFatG = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HealthConditionsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DietaryPreferencesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NutritionProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MealPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NutritionProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalCalories = table.Column<int>(type: "int", nullable: false),
                    TotalProteinG = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCarbsG = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalFatG = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MealsJson = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "[]"),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IsNotificationSent = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MealPlans_NutritionProfiles_NutritionProfileId",
                        column: x => x.NutritionProfileId,
                        principalTable: "NutritionProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NutritionProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WorkoutType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    EstimatedCaloriesBurned = table.Column<int>(type: "int", nullable: false),
                    ExercisesJson = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "[]"),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IsNotificationSent = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkoutPlans_NutritionProfiles_NutritionProfileId",
                        column: x => x.NutritionProfileId,
                        principalTable: "NutritionProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MealPlans_IsNotificationSent",
                table: "MealPlans",
                column: "IsNotificationSent");

            migrationBuilder.CreateIndex(
                name: "IX_MealPlans_NutritionProfileId",
                table: "MealPlans",
                column: "NutritionProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_MealPlans_UserId_Date",
                table: "MealPlans",
                columns: new[] { "UserId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_NutritionProfiles_CreatedAt",
                table: "NutritionProfiles",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_NutritionProfiles_UserId",
                table: "NutritionProfiles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutPlans_IsNotificationSent",
                table: "WorkoutPlans",
                column: "IsNotificationSent");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutPlans_NutritionProfileId",
                table: "WorkoutPlans",
                column: "NutritionProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutPlans_UserId_Date",
                table: "WorkoutPlans",
                columns: new[] { "UserId", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MealPlans");

            migrationBuilder.DropTable(
                name: "WorkoutPlans");

            migrationBuilder.DropTable(
                name: "NutritionProfiles");
        }
    }
}
