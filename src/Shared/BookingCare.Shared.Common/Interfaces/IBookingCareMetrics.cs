namespace BookingCare.Shared.Common.Interfaces;

/// <summary>
/// Interface for BookingCare service metrics
/// </summary>
public interface IBookingCareMetrics
{
    /// <summary>
    /// Increment operation counter
    /// </summary>
    void IncrementOperation(string operation, string result = "success");

    /// <summary>
    /// Record operation duration
    /// </summary>
    void RecordDuration(string operation, double durationSeconds);

    /// <summary>
    /// Set current active items gauge
    /// </summary>
    void SetActiveItems(string itemType, double count);

    /// <summary>
    /// Increment error counter
    /// </summary>
    void IncrementError(string errorType, string operation = "");

    /// <summary>
    /// Record database operation
    /// </summary>
    void RecordDatabaseOperation(string operation, string table, double durationMs);

    /// <summary>
    /// Record gRPC operation
    /// </summary>
    void RecordGrpcOperation(string method, string result, double durationMs);

    /// <summary>
    /// Record external API call
    /// </summary>
    void RecordExternalApiCall(string api, string endpoint, string statusCode, double durationMs);

    /// <summary>
    /// Record business metric
    /// </summary>
    void RecordBusinessMetric(string metricName, double value, params (string key, string value)[] labels);
}