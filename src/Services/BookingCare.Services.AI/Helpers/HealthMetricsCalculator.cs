using BookingCare.Services.AI.Models.DTOs;

namespace BookingCare.Services.AI.Helpers;

/// <summary>
/// Helper class for calculating health metrics (BMI, BMR, TDEE, macro targets)
/// </summary>
public class HealthMetricsCalculator
{
    /// <summary>
    /// Calculate all health metrics for a user
    /// </summary>
    public HealthMetricsDto CalculateMetrics(
        decimal heightCm,
        decimal weightKg,
        int age,
        string gender,
        string activityLevel,
        string healthGoal)
    {
        var bmi = CalculateBMI(weightKg, heightCm);
        var bmr = CalculateBMR(weightKg, heightCm, age, gender);
        var tdee = CalculateTDEE(bmr, activityLevel);
        var targetCalories = CalculateTargetCalories(tdee, healthGoal);
        var macros = CalculateMacroTargets(targetCalories, healthGoal);

        return new HealthMetricsDto
        {
            BMI = bmi,
            BMR = bmr,
            TDEE = tdee,
            TargetCalories = targetCalories,
            TargetProteinG = macros.Protein,
            TargetCarbsG = macros.Carbs,
            TargetFatG = macros.Fat,
            BMICategory = GetBMICategory(bmi)
        };
    }

    /// <summary>
    /// Calculate Body Mass Index
    /// </summary>
    public decimal CalculateBMI(decimal weightKg, decimal heightCm)
    {
        var heightM = heightCm / 100m;
        var bmi = weightKg / (heightM * heightM);
        return Math.Round(bmi, 2);
    }

    /// <summary>
    /// Calculate Basal Metabolic Rate using Mifflin-St Jeor Equation
    /// </summary>
    public decimal CalculateBMR(decimal weightKg, decimal heightCm, int age, string gender)
    {
        // Mifflin-St Jeor Equation:
        // Men: BMR = (10 × weight in kg) + (6.25 × height in cm) - (5 × age in years) + 5
        // Women: BMR = (10 × weight in kg) + (6.25 × height in cm) - (5 × age in years) - 161

        decimal bmr;
        if (gender.Equals("Male", StringComparison.OrdinalIgnoreCase))
        {
            bmr = (10m * weightKg) + (6.25m * heightCm) - (5m * age) + 5m;
        }
        else
        {
            bmr = (10m * weightKg) + (6.25m * heightCm) - (5m * age) - 161m;
        }

        return Math.Round(bmr, 2);
    }

    /// <summary>
    /// Calculate Total Daily Energy Expenditure
    /// </summary>
    public decimal CalculateTDEE(decimal bmr, string activityLevel)
    {
        var multiplier = activityLevel.ToLower() switch
        {
            "sedentary" => 1.2m,           // Little or no exercise
            "lightlyactive" => 1.375m,     // Light exercise 1-3 days/week
            "moderatelyactive" => 1.55m,   // Moderate exercise 3-5 days/week
            "veryactive" => 1.725m,        // Hard exercise 6-7 days/week
            "extraactive" => 1.9m,         // Very hard exercise & physical job
            _ => 1.2m                      // Default to sedentary
        };

        return Math.Round(bmr * multiplier, 2);
    }

    /// <summary>
    /// Calculate target calories based on health goal
    /// </summary>
    public int CalculateTargetCalories(decimal tdee, string healthGoal)
    {
        var targetCalories = healthGoal.ToLower() switch
        {
            "weightloss" => tdee * 0.8m,      // 20% deficit
            "musclegain" => tdee * 1.15m,     // 15% surplus
            "maintenance" => tdee,             // Maintain current weight
            _ => tdee                          // Default to maintenance
        };

        return (int)Math.Round(targetCalories);
    }

    /// <summary>
    /// Calculate macro targets (protein, carbs, fat) based on calories and goal
    /// </summary>
    public (decimal Protein, decimal Carbs, decimal Fat) CalculateMacroTargets(
        int targetCalories,
        string healthGoal)
    {
        // Macro distribution based on goal:
        // Weight Loss: High protein (35%), Moderate carbs (30%), Moderate fat (35%)
        // Muscle Gain: High protein (30%), High carbs (45%), Low fat (25%)
        // Maintenance: Balanced (25% protein, 45% carbs, 30% fat)

        decimal proteinPercent, carbsPercent, fatPercent;

        switch (healthGoal.ToLower())
        {
            case "weightloss":
                proteinPercent = 0.35m;
                carbsPercent = 0.30m;
                fatPercent = 0.35m;
                break;
            case "musclegain":
                proteinPercent = 0.30m;
                carbsPercent = 0.45m;
                fatPercent = 0.25m;
                break;
            case "maintenance":
            default:
                proteinPercent = 0.25m;
                carbsPercent = 0.45m;
                fatPercent = 0.30m;
                break;
        }

        // Calculate grams:
        // Protein: 4 calories per gram
        // Carbs: 4 calories per gram
        // Fat: 9 calories per gram

        var proteinG = (targetCalories * proteinPercent) / 4m;
        var carbsG = (targetCalories * carbsPercent) / 4m;
        var fatG = (targetCalories * fatPercent) / 9m;

        return (
            Math.Round(proteinG, 2),
            Math.Round(carbsG, 2),
            Math.Round(fatG, 2)
        );
    }

    /// <summary>
    /// Get BMI category
    /// </summary>
    public string GetBMICategory(decimal bmi)
    {
        return bmi switch
        {
            < 18.5m => "Underweight",
            >= 18.5m and < 25m => "Normal",
            >= 25m and < 30m => "Overweight",
            _ => "Obese"
        };
    }
}
