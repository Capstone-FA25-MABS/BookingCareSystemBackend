-- BookingCare Saga Database Setup Script
-- This script creates the MABS_Sagas database and all required objects
-- Execute this script in SQL Server Management Studio or sqlcmd

-- Step 1: Create database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'MABS_Sagas')
BEGIN
    CREATE DATABASE MABS_Sagas;
    PRINT 'Database MABS_Sagas created successfully.';
END
ELSE
BEGIN
    PRINT 'Database MABS_Sagas already exists.';
END

GO

-- Step 2: Use the database
USE MABS_Sagas;

GO

-- Step 3: Check if tables already exist and drop them if needed (for clean setup)
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SagaStepExecutions')
    DROP TABLE SagaStepExecutions;

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SagaEvents')
    DROP TABLE SagaEvents;

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SagaMetrics')
    DROP TABLE SagaMetrics;

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SagaStates')
    DROP TABLE SagaStates;

PRINT 'Existing tables cleaned up.';

GO

-- Step 4: Create all tables, views, and stored procedures
-- (This is the complete saga_schema.sql content)

-- Create Saga States table
CREATE TABLE SagaStates (
    SagaId UNIQUEIDENTIFIER PRIMARY KEY,
    SagaName NVARCHAR(255) NOT NULL,
    Status NVARCHAR(50) NOT NULL,
    ContextData NVARCHAR(MAX) NOT NULL, -- JSON serialized SagaContext
    CurrentStep NVARCHAR(255) NULL,
    CompletedSteps NVARCHAR(MAX) NULL, -- JSON array of completed step names
    CompensatedSteps NVARCHAR(MAX) NULL, -- JSON array of compensated step names
    CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(7) NULL,
    CompletedAt DATETIME2(7) NULL,
    ErrorMessage NVARCHAR(MAX) NULL,
    RetryCount INT NOT NULL DEFAULT 0,
    NextRetryAt DATETIME2(7) NULL,
    StepData NVARCHAR(MAX) NULL, -- JSON object for step-specific data
    CorrelationId NVARCHAR(255) NULL, -- For correlation with business events
    UserId NVARCHAR(255) NULL, -- For user-related sagas
    TenantId NVARCHAR(255) NULL, -- For multi-tenant scenarios
    Version ROWVERSION NOT NULL -- For optimistic concurrency
);

PRINT 'SagaStates table created.';

-- Create indexes for performance
CREATE INDEX IX_SagaStates_Status ON SagaStates (Status);
CREATE INDEX IX_SagaStates_SagaName ON SagaStates (SagaName);
CREATE INDEX IX_SagaStates_CreatedAt ON SagaStates (CreatedAt);
CREATE INDEX IX_SagaStates_NextRetryAt ON SagaStates (NextRetryAt) WHERE NextRetryAt IS NOT NULL;
CREATE INDEX IX_SagaStates_CorrelationId ON SagaStates (CorrelationId) WHERE CorrelationId IS NOT NULL;
CREATE INDEX IX_SagaStates_UserId ON SagaStates (UserId) WHERE UserId IS NOT NULL;

PRINT 'SagaStates indexes created.';

-- Create Saga Events table for audit trail
CREATE TABLE SagaEvents (
    EventId BIGINT IDENTITY(1,1) PRIMARY KEY,
    SagaId UNIQUEIDENTIFIER NOT NULL,
    EventType NVARCHAR(100) NOT NULL, -- Started, StepCompleted, StepFailed, Completed, Failed, etc.
    StepName NVARCHAR(255) NULL,
    EventData NVARCHAR(MAX) NULL, -- JSON event data
    CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    
    FOREIGN KEY (SagaId) REFERENCES SagaStates(SagaId) ON DELETE CASCADE
);

PRINT 'SagaEvents table created.';

-- Create indexes for saga events
CREATE INDEX IX_SagaEvents_SagaId ON SagaEvents (SagaId);
CREATE INDEX IX_SagaEvents_EventType ON SagaEvents (EventType);
CREATE INDEX IX_SagaEvents_CreatedAt ON SagaEvents (CreatedAt);

PRINT 'SagaEvents indexes created.';

-- Create Saga Step Executions table for detailed tracking
CREATE TABLE SagaStepExecutions (
    ExecutionId BIGINT IDENTITY(1,1) PRIMARY KEY,
    SagaId UNIQUEIDENTIFIER NOT NULL,
    StepName NVARCHAR(255) NOT NULL,
    AttemptNumber INT NOT NULL DEFAULT 1,
    Status NVARCHAR(50) NOT NULL, -- Running, Completed, Failed, Compensated
    StartedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    CompletedAt DATETIME2(7) NULL,
    ExecutionTimeMs BIGINT NULL,
    InputData NVARCHAR(MAX) NULL, -- JSON input data
    OutputData NVARCHAR(MAX) NULL, -- JSON output data
    ErrorMessage NVARCHAR(MAX) NULL,
    ErrorDetails NVARCHAR(MAX) NULL, -- Stack trace, etc.
    IsCompensation BIT NOT NULL DEFAULT 0,
    
    FOREIGN KEY (SagaId) REFERENCES SagaStates(SagaId) ON DELETE CASCADE
);

PRINT 'SagaStepExecutions table created.';

-- Create indexes for step executions
CREATE INDEX IX_SagaStepExecutions_SagaId ON SagaStepExecutions (SagaId);
CREATE INDEX IX_SagaStepExecutions_StepName ON SagaStepExecutions (StepName);
CREATE INDEX IX_SagaStepExecutions_Status ON SagaStepExecutions (Status);
CREATE INDEX IX_SagaStepExecutions_StartedAt ON SagaStepExecutions (StartedAt);

PRINT 'SagaStepExecutions indexes created.';

-- Create Saga Metrics table for monitoring
CREATE TABLE SagaMetrics (
    MetricId BIGINT IDENTITY(1,1) PRIMARY KEY,
    SagaName NVARCHAR(255) NOT NULL,
    MetricType NVARCHAR(100) NOT NULL, -- ExecutionTime, StepCount, FailureRate, etc.
    MetricValue DECIMAL(18,4) NOT NULL,
    TimeWindow NVARCHAR(50) NOT NULL, -- Daily, Hourly, etc.
    WindowStart DATETIME2(7) NOT NULL,
    WindowEnd DATETIME2(7) NOT NULL,
    CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE()
);

PRINT 'SagaMetrics table created.';

-- Create indexes for metrics
CREATE INDEX IX_SagaMetrics_SagaName_MetricType ON SagaMetrics (SagaName, MetricType);
CREATE INDEX IX_SagaMetrics_WindowStart ON SagaMetrics (WindowStart);

PRINT 'SagaMetrics indexes created.';

GO

-- Step 5: Create stored procedures

-- Get saga state with concurrency check
CREATE PROCEDURE GetSagaState
    @SagaId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        SagaId,
        SagaName,
        Status,
        ContextData,
        CurrentStep,
        CompletedSteps,
        CompensatedSteps,
        CreatedAt,
        UpdatedAt,
        CompletedAt,
        ErrorMessage,
        RetryCount,
        NextRetryAt,
        StepData,
        CorrelationId,
        UserId,
        TenantId,
        Version
    FROM SagaStates
    WHERE SagaId = @SagaId;
END;

PRINT 'GetSagaState procedure created.';

GO

-- Save new saga state
CREATE PROCEDURE SaveSagaState
    @SagaId UNIQUEIDENTIFIER,
    @SagaName NVARCHAR(255),
    @Status NVARCHAR(50),
    @ContextData NVARCHAR(MAX),
    @CurrentStep NVARCHAR(255) = NULL,
    @CompletedSteps NVARCHAR(MAX) = NULL,
    @CompensatedSteps NVARCHAR(MAX) = NULL,
    @ErrorMessage NVARCHAR(MAX) = NULL,
    @RetryCount INT = 0,
    @NextRetryAt DATETIME2(7) = NULL,
    @StepData NVARCHAR(MAX) = NULL,
    @CorrelationId NVARCHAR(255) = NULL,
    @UserId NVARCHAR(255) = NULL,
    @TenantId NVARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO SagaStates (
        SagaId, SagaName, Status, ContextData, CurrentStep, CompletedSteps,
        CompensatedSteps, ErrorMessage, RetryCount, NextRetryAt, StepData,
        CorrelationId, UserId, TenantId
    )
    VALUES (
        @SagaId, @SagaName, @Status, @ContextData, @CurrentStep, @CompletedSteps,
        @CompensatedSteps, @ErrorMessage, @RetryCount, @NextRetryAt, @StepData,
        @CorrelationId, @UserId, @TenantId
    );
    
    -- Log saga started event
    INSERT INTO SagaEvents (SagaId, EventType, EventData)
    VALUES (@SagaId, 'SagaStarted', CONCAT('{"SagaName":"', @SagaName, '","Status":"', @Status, '"}'));
END;

PRINT 'SaveSagaState procedure created.';

GO

-- Update existing saga state
CREATE PROCEDURE UpdateSagaState
    @SagaId UNIQUEIDENTIFIER,
    @Status NVARCHAR(50),
    @ContextData NVARCHAR(MAX),
    @CurrentStep NVARCHAR(255) = NULL,
    @CompletedSteps NVARCHAR(MAX) = NULL,
    @CompensatedSteps NVARCHAR(MAX) = NULL,
    @ErrorMessage NVARCHAR(MAX) = NULL,
    @RetryCount INT = 0,
    @NextRetryAt DATETIME2(7) = NULL,
    @StepData NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE SagaStates
    SET 
        Status = @Status,
        ContextData = @ContextData,
        CurrentStep = @CurrentStep,
        CompletedSteps = @CompletedSteps,
        CompensatedSteps = @CompensatedSteps,
        UpdatedAt = GETUTCDATE(),
        CompletedAt = CASE WHEN @Status IN ('Completed', 'Compensated', 'Failed', 'Cancelled') THEN GETUTCDATE() ELSE CompletedAt END,
        ErrorMessage = @ErrorMessage,
        RetryCount = @RetryCount,
        NextRetryAt = @NextRetryAt,
        StepData = @StepData
    WHERE SagaId = @SagaId;
    
    -- Log saga status change event
    INSERT INTO SagaEvents (SagaId, EventType, EventData)
    VALUES (@SagaId, 'StatusChanged', CONCAT('{"NewStatus":"', @Status, '","CurrentStep":"', ISNULL(@CurrentStep, ''), '"}'));
END;

PRINT 'UpdateSagaState procedure created.';

GO

-- Get pending sagas for processing
CREATE PROCEDURE GetPendingSagas
    @MaxCount INT = 100
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT TOP (@MaxCount)
        SagaId as SagaId,
        SagaName,
        Status,
        ContextData,
        CurrentStep,
        CompletedSteps,
        CompensatedSteps,
        CreatedAt,
        UpdatedAt,
        CompletedAt,
        ErrorMessage,
        RetryCount,
        NextRetryAt,
        StepData,
        CorrelationId,
        UserId,
        TenantId,
        Version
    FROM SagaStates
    WHERE Status IN ('Pending', 'Running')
       OR (Status = 'Failed' AND NextRetryAt IS NOT NULL AND NextRetryAt <= GETUTCDATE())
    ORDER BY CreatedAt;
END;

PRINT 'GetPendingSagas procedure created.';

GO

-- Log saga step execution
CREATE PROCEDURE LogSagaStepExecution
    @SagaId UNIQUEIDENTIFIER,
    @StepName NVARCHAR(255),
    @AttemptNumber INT,
    @Status NVARCHAR(50),
    @StartedAt DATETIME2(7),
    @CompletedAt DATETIME2(7) = NULL,
    @ExecutionTimeMs BIGINT = NULL,
    @InputData NVARCHAR(MAX) = NULL,
    @OutputData NVARCHAR(MAX) = NULL,
    @ErrorMessage NVARCHAR(MAX) = NULL,
    @ErrorDetails NVARCHAR(MAX) = NULL,
    @IsCompensation BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO SagaStepExecutions (
        SagaId, StepName, AttemptNumber, Status, StartedAt, CompletedAt,
        ExecutionTimeMs, InputData, OutputData, ErrorMessage, ErrorDetails,
        IsCompensation
    )
    VALUES (
        @SagaId, @StepName, @AttemptNumber, @Status, @StartedAt, @CompletedAt,
        @ExecutionTimeMs, @InputData, @OutputData, @ErrorMessage, @ErrorDetails,
        @IsCompensation
    );
END;

PRINT 'LogSagaStepExecution procedure created.';

GO

-- Cleanup completed sagas older than specified days
CREATE PROCEDURE CleanupCompletedSagas
    @RetentionDays INT = 30
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @CutoffDate DATETIME2(7) = DATEADD(DAY, -@RetentionDays, GETUTCDATE());
    DECLARE @DeletedCount INT;
    
    DELETE FROM SagaStates
    WHERE Status IN ('Completed', 'Compensated', 'Cancelled')
      AND CompletedAt < @CutoffDate;
    
    SET @DeletedCount = @@ROWCOUNT;
    
    SELECT @DeletedCount as DeletedSagasCount;
END;

PRINT 'CleanupCompletedSagas procedure created.';

GO