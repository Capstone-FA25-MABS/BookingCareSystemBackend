-- Migration: Add fields for Nutrition Roadmap feature
-- Date: 2025-12-17

-- Add new fields to NutritionProfiles table
-- Note: Age and Gender are fetched from User service, not stored here

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NutritionProfiles]') AND name = 'BMR')
BEGIN
    ALTER TABLE [dbo].[NutritionProfiles]
    ADD [BMR] DECIMAL(18, 2) NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NutritionProfiles]') AND name = 'TDEE')
BEGIN
    ALTER TABLE [dbo].[NutritionProfiles]
    ADD [TDEE] DECIMAL(18, 2) NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NutritionProfiles]') AND name = 'StreakCount')
BEGIN
    ALTER TABLE [dbo].[NutritionProfiles]
    ADD [StreakCount] INT NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NutritionProfiles]') AND name = 'LastCompletedDate')
BEGIN
    ALTER TABLE [dbo].[NutritionProfiles]
    ADD [LastCompletedDate] DATETIME2 NULL;
END
GO

-- Add new fields to MealPlans table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[MealPlans]') AND name = 'CompletedItemsJson')
BEGIN
    ALTER TABLE [dbo].[MealPlans]
    ADD [CompletedItemsJson] NVARCHAR(MAX) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[MealPlans]') AND name = 'IsFullyCompleted')
BEGIN
    ALTER TABLE [dbo].[MealPlans]
    ADD [IsFullyCompleted] BIT NOT NULL DEFAULT 0;
END
GO

-- Add new fields to WorkoutPlans table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[WorkoutPlans]') AND name = 'CompletedItemsJson')
BEGIN
    ALTER TABLE [dbo].[WorkoutPlans]
    ADD [CompletedItemsJson] NVARCHAR(MAX) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[WorkoutPlans]') AND name = 'IsFullyCompleted')
BEGIN
    ALTER TABLE [dbo].[WorkoutPlans]
    ADD [IsFullyCompleted] BIT NOT NULL DEFAULT 0;
END
GO

PRINT 'Migration completed successfully: AddNutritionRoadmapFields';
GO
