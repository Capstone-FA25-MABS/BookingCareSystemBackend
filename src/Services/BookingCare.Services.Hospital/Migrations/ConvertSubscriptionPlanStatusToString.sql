-- Migration script to convert subscription_plans.status from int to nvarchar
-- Run this script directly on your database if migration doesn't work

-- Step 1: Drop check constraint
IF EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_subscription_plans_status')
BEGIN
    ALTER TABLE subscription_plans DROP CONSTRAINT CK_subscription_plans_status;
END
GO

-- Step 2: Add temporary column
ALTER TABLE subscription_plans ADD status_temp NVARCHAR(20) NULL;
GO

-- Step 3: Convert existing int values to string
UPDATE subscription_plans 
SET status_temp = CASE 
    WHEN status = 0 THEN 'ACTIVE'
    WHEN status = 1 THEN 'INACTIVE'
    ELSE 'ACTIVE'
END;
GO

-- Step 4: Drop old int column
ALTER TABLE subscription_plans DROP COLUMN status;
GO

-- Step 5: Rename temp column to status
EXEC sp_rename 'subscription_plans.status_temp', 'status', 'COLUMN';
GO

-- Step 6: Make column NOT NULL with default value
ALTER TABLE subscription_plans 
ALTER COLUMN status NVARCHAR(20) NOT NULL;
GO

ALTER TABLE subscription_plans 
ADD CONSTRAINT DF_subscription_plans_status DEFAULT 'ACTIVE' FOR status;
GO

-- Step 7: Recreate check constraint
ALTER TABLE subscription_plans
ADD CONSTRAINT CK_subscription_plans_status 
CHECK (status IN ('ACTIVE', 'INACTIVE'));
GO

