-- Migration: Rename UserId to AccountId in NutritionProfiles, MealPlans, WorkoutPlans
-- Date: 2025-12-17
-- Reason: UserId thực tế lưu AccountId, đổi tên cho đúng nghĩa

-- Bước 1: Đổi tên column trong NutritionProfiles
EXEC sp_rename 'NutritionProfiles.UserId', 'AccountId', 'COLUMN';

-- Bước 2: Đổi tên column trong MealPlans
EXEC sp_rename 'MealPlans.UserId', 'AccountId', 'COLUMN';

-- Bước 3: Đổi tên column trong WorkoutPlans
EXEC sp_rename 'WorkoutPlans.UserId', 'AccountId', 'COLUMN';

-- Bước 4: Đổi tên index (nếu có)
-- NutritionProfiles
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_NutritionProfiles_UserId')
BEGIN
    EXEC sp_rename 'NutritionProfiles.IX_NutritionProfiles_UserId', 'IX_NutritionProfiles_AccountId', 'INDEX';
END

-- MealPlans
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MealPlans_UserId')
BEGIN
    EXEC sp_rename 'MealPlans.IX_MealPlans_UserId', 'IX_MealPlans_AccountId', 'INDEX';
END

-- WorkoutPlans
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_WorkoutPlans_UserId')
BEGIN
    EXEC sp_rename 'WorkoutPlans.IX_WorkoutPlans_UserId', 'IX_WorkoutPlans_AccountId', 'INDEX';
END

-- Bước 5: Kiểm tra kết quả
SELECT 
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE COLUMN_NAME = 'AccountId'
AND TABLE_NAME IN ('NutritionProfiles', 'MealPlans', 'WorkoutPlans');

PRINT 'Migration completed successfully!';
