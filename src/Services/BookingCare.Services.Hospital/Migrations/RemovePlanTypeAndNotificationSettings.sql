-- Migration script to remove plan_type and notification_settings columns from subscription_plans table
-- Run this script directly on your database

USE MABS_Hospital;
GO

-- Step 1: Drop plan_type column if it exists
IF EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'subscription_plans') 
    AND name = 'plan_type'
)
BEGIN
    -- Drop default constraint first if exists
    DECLARE @ConstraintName NVARCHAR(200)
    SELECT @ConstraintName = name
    FROM sys.default_constraints
    WHERE parent_object_id = OBJECT_ID('subscription_plans')
    AND parent_column_id = COLUMNPROPERTY(OBJECT_ID('subscription_plans'), 'plan_type', 'ColumnId')
    
    IF @ConstraintName IS NOT NULL
    BEGIN
        EXEC('ALTER TABLE subscription_plans DROP CONSTRAINT ' + @ConstraintName)
        PRINT 'Dropped default constraint for plan_type';
    END
    
    -- Drop check constraint if exists
    IF EXISTS (
        SELECT * FROM sys.check_constraints 
        WHERE name = 'CK_subscription_plans_plan_type'
    )
    BEGIN
        ALTER TABLE subscription_plans DROP CONSTRAINT CK_subscription_plans_plan_type;
        PRINT 'Dropped check constraint CK_subscription_plans_plan_type';
    END
    
    -- Now drop the column
    ALTER TABLE subscription_plans DROP COLUMN plan_type;
    PRINT 'Dropped plan_type column';
END
ELSE
BEGIN
    PRINT 'plan_type column does not exist, skipping';
END
GO

-- Step 2: Drop notification_settings column if it exists
IF EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'subscription_plans') 
    AND name = 'notification_settings'
)
BEGIN
    ALTER TABLE subscription_plans DROP COLUMN notification_settings;
    PRINT 'Dropped notification_settings column';
END
ELSE
BEGIN
    PRINT 'notification_settings column does not exist, skipping';
END
GO

PRINT 'Migration completed successfully!';
GO
