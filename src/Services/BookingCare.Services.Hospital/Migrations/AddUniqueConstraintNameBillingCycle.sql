-- Migration script to add unique constraint on (name, billing_cycle) in subscription_plans table
-- This allows same plan name with different billing cycles
-- Run this script on MABS_Hospital database

USE MABS_Hospital;
GO

-- Step 1: Drop the old unique index on name only (if it exists)
IF EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'IX_subscription_plans_name' 
    AND object_id = OBJECT_ID(N'subscription_plans')
)
BEGIN
    DROP INDEX IX_subscription_plans_name ON subscription_plans;
    PRINT 'Dropped old unique index IX_subscription_plans_name on name column';
END
ELSE
BEGIN
    PRINT 'Old unique index IX_subscription_plans_name does not exist';
END
GO

-- Step 2: Check if new composite unique index already exists and drop it if needed
IF EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'IX_subscription_plans_name_billing_cycle_unique' 
    AND object_id = OBJECT_ID(N'subscription_plans')
)
BEGIN
    DROP INDEX IX_subscription_plans_name_billing_cycle_unique ON subscription_plans;
    PRINT 'Dropped existing unique index IX_subscription_plans_name_billing_cycle_unique';
END
ELSE
BEGIN
    PRINT 'Unique index IX_subscription_plans_name_billing_cycle_unique does not exist yet';
END
GO

-- Step 3: Create unique index on (name, billing_cycle)
CREATE UNIQUE INDEX IX_subscription_plans_name_billing_cycle_unique
ON subscription_plans (name, billing_cycle);
PRINT 'Created unique index IX_subscription_plans_name_billing_cycle_unique';
GO

PRINT 'Migration completed successfully!';
PRINT 'Now you can create multiple subscription plans with the same name but different billing cycles.';
PRINT 'Example: "Gói Nâng Cao" with MONTHLY, "Gói Nâng Cao" with QUARTERLY, "Gói Nâng Cao" with YEARLY';
PRINT 'But you cannot create two plans with the same name AND same billing cycle.';
GO

