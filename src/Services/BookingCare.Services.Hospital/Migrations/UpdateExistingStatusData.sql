-- Update existing data if any records have int status values
-- This should already be handled by migration, but running as safety check
UPDATE subscription_plans 
SET status = CASE 
    WHEN status = '0' THEN 'ACTIVE'
    WHEN status = '1' THEN 'INACTIVE'
    WHEN status IS NULL THEN 'ACTIVE'
    ELSE status
END
WHERE status IN ('0', '1') OR status IS NULL;
GO

-- Verify the update
SELECT id, name, status, 
    CASE 
        WHEN status = 'ACTIVE' THEN '✓ Valid'
        WHEN status = 'INACTIVE' THEN '✓ Valid'
        ELSE '✗ Invalid'
    END AS status_check
FROM subscription_plans;
GO

