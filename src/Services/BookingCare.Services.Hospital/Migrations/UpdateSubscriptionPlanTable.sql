-- Migration script to update subscription_plans table in MABS_Hospital database
-- This script safely updates existing table without losing data

-- Step 1: Drop auto_renew column if it exists
IF EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'subscription_plans') 
    AND name = 'auto_renew'
)
BEGIN
    ALTER TABLE subscription_plans DROP COLUMN auto_renew;
    PRINT 'Dropped auto_renew column';
END
ELSE
BEGIN
    PRINT 'auto_renew column does not exist, skipping';
END
GO

-- Step 2: Drop existing constraint if exists (in case it references plan_type before adding)
IF EXISTS (
    SELECT * FROM sys.check_constraints 
    WHERE name = 'CK_subscription_plans_plan_type'
)
BEGIN
    ALTER TABLE subscription_plans DROP CONSTRAINT CK_subscription_plans_plan_type;
    PRINT 'Dropped existing CK_subscription_plans_plan_type constraint';
END
GO

-- Step 3: Add plan_type column if it doesn't exist
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'subscription_plans') 
    AND name = 'plan_type'
)
BEGIN
    ALTER TABLE subscription_plans 
    ADD plan_type INT NOT NULL DEFAULT 0;
    
    PRINT 'Added plan_type column with default value 0 (TRIAL)';
END
ELSE
BEGIN
    PRINT 'plan_type column already exists, skipping';
END
GO

-- Step 4: Add check constraint for plan_type after column is created
IF EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'subscription_plans') 
    AND name = 'plan_type'
)
AND NOT EXISTS (
    SELECT * FROM sys.check_constraints 
    WHERE name = 'CK_subscription_plans_plan_type'
)
BEGIN
    ALTER TABLE subscription_plans
    ADD CONSTRAINT CK_subscription_plans_plan_type 
    CHECK (plan_type IN (0, 1, 2));
    
    PRINT 'Added check constraint CK_subscription_plans_plan_type';
END
ELSE
BEGIN
    PRINT 'plan_type constraint already exists or column missing, skipping';
END
GO

-- Step 5: Add notification_settings column if it doesn't exist
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'subscription_plans') 
    AND name = 'notification_settings'
)
BEGIN
    ALTER TABLE subscription_plans 
    ADD notification_settings NVARCHAR(MAX) NULL;
    
    PRINT 'Added notification_settings column';
END
ELSE
BEGIN
    PRINT 'notification_settings column already exists, skipping';
END
GO

-- Step 6: Add max_appointments column if it doesn't exist
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'subscription_plans') 
    AND name = 'max_appointments'
)
BEGIN
    ALTER TABLE subscription_plans 
    ADD max_appointments INT NOT NULL DEFAULT 0;
    
    PRINT 'Added max_appointments column with default value 0';
END
ELSE
BEGIN
    PRINT 'max_appointments column already exists, skipping';
END
GO

-- Step 7: Add check constraint for max_appointments if column exists
IF EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'subscription_plans') 
    AND name = 'max_appointments'
)
AND NOT EXISTS (
    SELECT * FROM sys.check_constraints 
    WHERE name = 'CK_subscription_plans_max_appointments'
)
BEGIN
    ALTER TABLE subscription_plans
    ADD CONSTRAINT CK_subscription_plans_max_appointments 
    CHECK (max_appointments >= 0);
    
    PRINT 'Added check constraint CK_subscription_plans_max_appointments';
END
GO

-- Step 8: Update existing data - Set default notification_settings for existing plans
UPDATE subscription_plans
SET notification_settings = '{"EnableRenewalReminders":true,"ReminderDaysBeforeExpiry":[7,5,3,1],"EnableUpgradePromotion":true,"UpgradePromotionDaysBeforeExpiry":7}'
WHERE notification_settings IS NULL;
GO

PRINT 'Migration completed successfully!';
GO
