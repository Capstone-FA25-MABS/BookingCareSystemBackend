-- BookingCare Saga State Database Schema
-- Version: 1.0.0
-- Date: 2025-09-08

-- Create database if not exists (optional - usually done separately)
-- CREATE DATABASE BookingCareSaga;
-- GO

-- USE BookingCareSaga;
-- GO

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

-- Create indexes for performance
CREATE INDEX IX_SagaStates_Status ON SagaStates (Status);
CREATE INDEX IX_SagaStates_SagaName ON SagaStates (SagaName);
CREATE INDEX IX_SagaStates_CreatedAt ON SagaStates (CreatedAt);
CREATE INDEX IX_SagaStates_NextRetryAt ON SagaStates (NextRetryAt) WHERE NextRetryAt IS NOT NULL;
CREATE INDEX IX_SagaStates_CorrelationId ON SagaStates (CorrelationId) WHERE CorrelationId IS NOT NULL;
CREATE INDEX IX_SagaStates_UserId ON SagaStates (UserId) WHERE UserId IS NOT NULL;

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

-- Create indexes for saga events
CREATE INDEX IX_SagaEvents_SagaId ON SagaEvents (SagaId);
CREATE INDEX IX_SagaEvents_EventType ON SagaEvents (EventType);
CREATE INDEX IX_SagaEvents_CreatedAt ON SagaEvents (CreatedAt);

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

-- Create indexes for step executions
CREATE INDEX IX_SagaStepExecutions_SagaId ON SagaStepExecutions (SagaId);
CREATE INDEX IX_SagaStepExecutions_StepName ON SagaStepExecutions (StepName);
CREATE INDEX IX_SagaStepExecutions_Status ON SagaStepExecutions (Status);
CREATE INDEX IX_SagaStepExecutions_StartedAt ON SagaStepExecutions (StartedAt);

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

-- Create indexes for metrics
CREATE INDEX IX_SagaMetrics_SagaName_MetricType ON SagaMetrics (SagaName, MetricType);
CREATE INDEX IX_SagaMetrics_WindowStart ON SagaMetrics (WindowStart);

-- Create views for common queries
GO

-- View for pending sagas that need processing
CREATE VIEW PendingSagas AS
SELECT 
    SagaId,
    SagaName,
    Status,
    CurrentStep,
    CreatedAt,
    UpdatedAt,
    RetryCount,
    NextRetryAt,
    CorrelationId,
    UserId
FROM SagaStates
WHERE Status IN ('Pending', 'Running') 
   OR (Status = 'Failed' AND NextRetryAt IS NOT NULL AND NextRetryAt <= GETUTCDATE());

GO

-- View for saga execution summary
CREATE VIEW SagaExecutionSummary AS
SELECT 
    s.SagaId,
    s.SagaName,
    s.Status,
    s.CreatedAt,
    s.CompletedAt,
    DATEDIFF(MILLISECOND, s.CreatedAt, ISNULL(s.CompletedAt, GETUTCDATE())) AS ExecutionTimeMs,
    (SELECT COUNT(*) FROM SagaStepExecutions se WHERE se.SagaId = s.SagaId AND se.Status = 'Completed' AND se.IsCompensation = 0) AS CompletedSteps,
    (SELECT COUNT(*) FROM SagaStepExecutions se WHERE se.SagaId = s.SagaId AND se.Status = 'Failed') AS FailedSteps,
    (SELECT COUNT(*) FROM SagaStepExecutions se WHERE se.SagaId = s.SagaId AND se.IsCompensation = 1) AS CompensatedSteps,
    s.CorrelationId,
    s.UserId
FROM SagaStates s;

GO

-- Stored procedures for common operations

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
    VALUES (@SagaId, 'SagaStarted', JSON_OBJECT('SagaName', @SagaName, 'Status', @Status));
END;

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
    @StepData NVARCHAR(MAX) = NULL,
    @ExpectedVersion ROWVERSION
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @CurrentVersion ROWVERSION;
    DECLARE @RowsAffected INT;
    
    -- Check current version
    SELECT @CurrentVersion = Version FROM SagaStates WHERE SagaId = @SagaId;
    
    IF @CurrentVersion != @ExpectedVersion
    BEGIN
        RAISERROR('Concurrency conflict: Saga state was modified by another process', 16, 1);
        RETURN;
    END;
    
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
    WHERE SagaId = @SagaId AND Version = @ExpectedVersion;
    
    SET @RowsAffected = @@ROWCOUNT;
    
    IF @RowsAffected = 0
    BEGIN
        RAISERROR('Saga state update failed - concurrency conflict or saga not found', 16, 1);
        RETURN;
    END;
    
    -- Log saga status change event
    INSERT INTO SagaEvents (SagaId, EventType, EventData)
    VALUES (@SagaId, 'StatusChanged', JSON_OBJECT('NewStatus', @Status, 'CurrentStep', @CurrentStep));
END;

GO

-- Get pending sagas for processing
CREATE PROCEDURE GetPendingSagas
    @MaxCount INT = 100
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT TOP (@MaxCount)
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
    WHERE Status IN ('Pending', 'Running')
       OR (Status = 'Failed' AND NextRetryAt IS NOT NULL AND NextRetryAt <= GETUTCDATE())
    ORDER BY CreatedAt;
END;

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

GO

-- Grant permissions (adjust based on your security requirements)
-- CREATE USER [BookingCareSagaUser] FOR LOGIN [BookingCareSagaLogin];
-- GRANT SELECT, INSERT, UPDATE, DELETE ON SagaStates TO [BookingCareSagaUser];
-- GRANT SELECT, INSERT ON SagaEvents TO [BookingCareSagaUser];
-- GRANT SELECT, INSERT ON SagaStepExecutions TO [BookingCareSagaUser];
-- GRANT EXECUTE ON GetSagaState TO [BookingCareSagaUser];
-- GRANT EXECUTE ON SaveSagaState TO [BookingCareSagaUser];
-- GRANT EXECUTE ON UpdateSagaState TO [BookingCareSagaUser];
-- GRANT EXECUTE ON GetPendingSagas TO [BookingCareSagaUser];
-- GRANT EXECUTE ON LogSagaStepExecution TO [BookingCareSagaUser];

PRINT 'BookingCare Saga database schema created successfully!';
