using BookingCare.Shared.Saga.Abstractions;
using BookingCare.Shared.Saga.Models;
using Microsoft.Extensions.Logging;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace BookingCare.Shared.Saga.StateStore;

/// <summary>
/// SQL Server implementation of saga state store with optimistic concurrency control
/// </summary>
public class SqlServerSagaStateStore : ISagaStateStore
{
    private readonly string _connectionString;
    private readonly ILogger<SqlServerSagaStateStore> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public SqlServerSagaStateStore(string connectionString, ILogger<SqlServerSagaStateStore> logger)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<SagaState?> GetSagaStateAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting saga state for SagaId: {SagaId}", sagaId);

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand("GetSagaState", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@SagaId", sagaId);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (await reader.ReadAsync(cancellationToken))
            {
                return MapReaderToSagaState(reader);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting saga state for SagaId: {SagaId}", sagaId);
            throw;
        }
    }

    public async Task SaveSagaStateAsync(SagaState sagaState, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Saving saga state for SagaId: {SagaId}", sagaState.SagaId);

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand("SaveSagaState", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            AddSagaStateParameters(command, sagaState);

            await command.ExecuteNonQueryAsync(cancellationToken);

            _logger.LogInformation("Saga state saved successfully for SagaId: {SagaId}", sagaState.SagaId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving saga state for SagaId: {SagaId}", sagaState.SagaId);
            throw;
        }
    }

    public async Task UpdateSagaStateAsync(SagaState sagaState, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Updating saga state for SagaId: {SagaId}", sagaState.SagaId);

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            // First get the current version for optimistic concurrency
            var currentState = await GetSagaStateAsync(sagaState.SagaId, cancellationToken);
            if (currentState == null)
            {
                throw new InvalidOperationException($"Saga state not found for SagaId: {sagaState.SagaId}");
            }

            using var command = new SqlCommand("UpdateSagaState", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            AddUpdateSagaStateParameters(command, sagaState);
            // Add version parameter for optimistic concurrency (this would need to be tracked in SagaState)
            // command.Parameters.AddWithValue("@ExpectedVersion", currentState.Version);

            await command.ExecuteNonQueryAsync(cancellationToken);

            _logger.LogInformation("Saga state updated successfully for SagaId: {SagaId}", sagaState.SagaId);
        }
        catch (SqlException ex) when (ex.Message.Contains("Concurrency conflict"))
        {
            _logger.LogWarning("Concurrency conflict updating saga state for SagaId: {SagaId}", sagaState.SagaId);
            throw new InvalidOperationException("Saga state was modified by another process", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating saga state for SagaId: {SagaId}", sagaState.SagaId);
            throw;
        }
    }

    public async Task DeleteSagaStateAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Deleting saga state for SagaId: {SagaId}", sagaId);

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand("DELETE FROM SagaStates WHERE SagaId = @SagaId", connection);
            command.Parameters.AddWithValue("@SagaId", sagaId);

            var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

            if (rowsAffected > 0)
            {
                _logger.LogInformation("Saga state deleted successfully for SagaId: {SagaId}", sagaId);
            }
            else
            {
                _logger.LogWarning("No saga state found to delete for SagaId: {SagaId}", sagaId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting saga state for SagaId: {SagaId}", sagaId);
            throw;
        }
    }

    public async Task<IEnumerable<SagaState>> GetPendingSagasAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting pending sagas");

            var pendingSagas = new List<SagaState>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand("GetPendingSagas", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@MaxCount", 1000); // Configurable limit

            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                pendingSagas.Add(MapReaderToSagaState(reader));
            }

            _logger.LogInformation("Retrieved {Count} pending sagas", pendingSagas.Count);
            return pendingSagas;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending sagas");
            throw;
        }
    }

    public async Task LogSagaStepExecutionAsync(Guid sagaId, string stepName, int attemptNumber,
        string status, DateTime startedAt, DateTime? completedAt = null, long? executionTimeMs = null,
        string? inputData = null, string? outputData = null, string? errorMessage = null,
        string? errorDetails = null, bool isCompensation = false, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand("LogSagaStepExecution", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@SagaId", sagaId);
            command.Parameters.AddWithValue("@StepName", stepName);
            command.Parameters.AddWithValue("@AttemptNumber", attemptNumber);
            command.Parameters.AddWithValue("@Status", status);
            command.Parameters.AddWithValue("@StartedAt", startedAt);
            command.Parameters.AddWithValue("@CompletedAt", (object?)completedAt ?? DBNull.Value);
            command.Parameters.AddWithValue("@ExecutionTimeMs", (object?)executionTimeMs ?? DBNull.Value);
            command.Parameters.AddWithValue("@InputData", (object?)inputData ?? DBNull.Value);
            command.Parameters.AddWithValue("@OutputData", (object?)outputData ?? DBNull.Value);
            command.Parameters.AddWithValue("@ErrorMessage", (object?)errorMessage ?? DBNull.Value);
            command.Parameters.AddWithValue("@ErrorDetails", (object?)errorDetails ?? DBNull.Value);
            command.Parameters.AddWithValue("@IsCompensation", isCompensation);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging saga step execution for SagaId: {SagaId}, Step: {StepName}",
                sagaId, stepName);
            // Don't throw - logging failures shouldn't break saga execution
        }
    }

    public async Task CleanupCompletedSagasAsync(int retentionDays = 30, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting cleanup of completed sagas older than {RetentionDays} days", retentionDays);

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand("CleanupCompletedSagas", connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 300 // 5 minutes timeout for cleanup operation
            };

            command.Parameters.AddWithValue("@RetentionDays", retentionDays);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (await reader.ReadAsync(cancellationToken))
            {
                var deletedCount = reader.GetInt32("DeletedSagasCount");
                _logger.LogInformation("Cleaned up {DeletedCount} completed sagas", deletedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during saga cleanup");
            throw;
        }
    }

    private SagaState MapReaderToSagaState(SqlDataReader reader)
    {
        var contextJson = reader.GetString("ContextData");
        var context = JsonSerializer.Deserialize<SagaContext>(contextJson, _jsonOptions) ?? new SagaContext();

        var completedStepsJson = reader.IsDBNull("CompletedSteps") ? null : reader.GetString("CompletedSteps");
        var completedSteps = string.IsNullOrEmpty(completedStepsJson)
            ? new List<string>()
            : JsonSerializer.Deserialize<List<string>>(completedStepsJson, _jsonOptions) ?? new List<string>();

        var compensatedStepsJson = reader.IsDBNull("CompensatedSteps") ? null : reader.GetString("CompensatedSteps");
        var compensatedSteps = string.IsNullOrEmpty(compensatedStepsJson)
            ? new List<string>()
            : JsonSerializer.Deserialize<List<string>>(compensatedStepsJson, _jsonOptions) ?? new List<string>();

        var stepDataJson = reader.IsDBNull("StepData") ? null : reader.GetString("StepData");
        var stepData = string.IsNullOrEmpty(stepDataJson)
            ? new Dictionary<string, object>()
            : JsonSerializer.Deserialize<Dictionary<string, object>>(stepDataJson, _jsonOptions) ?? new Dictionary<string, object>();

        return new SagaState
        {
            SagaId = reader.GetGuid("SagaId"),
            SagaName = reader.GetString("SagaName"),
            Status = Enum.Parse<SagaStatus>(reader.GetString("Status")),
            Context = context,
            CurrentStep = reader.IsDBNull("CurrentStep") ? string.Empty : reader.GetString("CurrentStep"),
            CompletedSteps = completedSteps,
            CompensatedSteps = compensatedSteps,
            CreatedAt = reader.GetDateTime("CreatedAt"),
            UpdatedAt = reader.IsDBNull("UpdatedAt") ? null : reader.GetDateTime("UpdatedAt"),
            CompletedAt = reader.IsDBNull("CompletedAt") ? null : reader.GetDateTime("CompletedAt"),
            ErrorMessage = reader.IsDBNull("ErrorMessage") ? null : reader.GetString("ErrorMessage"),
            RetryCount = reader.GetInt32("RetryCount"),
            NextRetryAt = reader.IsDBNull("NextRetryAt") ? null : reader.GetDateTime("NextRetryAt"),
            StepData = stepData
        };
    }

    private void AddSagaStateParameters(SqlCommand command, SagaState sagaState)
    {
        command.Parameters.AddWithValue("@SagaId", sagaState.SagaId);
        command.Parameters.AddWithValue("@SagaName", sagaState.SagaName);
        command.Parameters.AddWithValue("@Status", sagaState.Status.ToString());
        command.Parameters.AddWithValue("@ContextData", JsonSerializer.Serialize(sagaState.Context, _jsonOptions));
        command.Parameters.AddWithValue("@CurrentStep", (object?)sagaState.CurrentStep ?? DBNull.Value);
        command.Parameters.AddWithValue("@CompletedSteps", JsonSerializer.Serialize(sagaState.CompletedSteps, _jsonOptions));
        command.Parameters.AddWithValue("@CompensatedSteps", JsonSerializer.Serialize(sagaState.CompensatedSteps, _jsonOptions));
        command.Parameters.AddWithValue("@ErrorMessage", (object?)sagaState.ErrorMessage ?? DBNull.Value);
        command.Parameters.AddWithValue("@RetryCount", sagaState.RetryCount);
        command.Parameters.AddWithValue("@NextRetryAt", (object?)sagaState.NextRetryAt ?? DBNull.Value);
        command.Parameters.AddWithValue("@StepData", JsonSerializer.Serialize(sagaState.StepData, _jsonOptions));
        command.Parameters.AddWithValue("@CorrelationId", (object?)sagaState.Context.CorrelationId ?? DBNull.Value);
        command.Parameters.AddWithValue("@UserId", (object?)sagaState.Context.UserId ?? DBNull.Value);
        command.Parameters.AddWithValue("@TenantId", DBNull.Value); // Add tenant support if needed
    }

    private void AddUpdateSagaStateParameters(SqlCommand command, SagaState sagaState)
    {
        command.Parameters.AddWithValue("@SagaId", sagaState.SagaId);
        command.Parameters.AddWithValue("@Status", sagaState.Status.ToString());
        command.Parameters.AddWithValue("@ContextData", JsonSerializer.Serialize(sagaState.Context, _jsonOptions));
        command.Parameters.AddWithValue("@CurrentStep", (object?)sagaState.CurrentStep ?? DBNull.Value);
        command.Parameters.AddWithValue("@CompletedSteps", JsonSerializer.Serialize(sagaState.CompletedSteps, _jsonOptions));
        command.Parameters.AddWithValue("@CompensatedSteps", JsonSerializer.Serialize(sagaState.CompensatedSteps, _jsonOptions));
        command.Parameters.AddWithValue("@ErrorMessage", (object?)sagaState.ErrorMessage ?? DBNull.Value);
        command.Parameters.AddWithValue("@RetryCount", sagaState.RetryCount);
        command.Parameters.AddWithValue("@NextRetryAt", (object?)sagaState.NextRetryAt ?? DBNull.Value);
        command.Parameters.AddWithValue("@StepData", JsonSerializer.Serialize(sagaState.StepData, _jsonOptions));
        // Note: ExpectedVersion parameter would be added here for optimistic concurrency
    }
}