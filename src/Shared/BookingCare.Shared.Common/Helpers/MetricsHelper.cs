using BookingCare.Shared.Common.Interfaces;
using System.Diagnostics;

namespace BookingCare.Shared.Common.Helpers;

/// <summary>
/// Helper for tracking database operations with metrics
/// </summary>
public static class DatabaseMetricsHelper
{
    /// <summary>
    /// Execute database operation with automatic metrics tracking
    /// </summary>
    public static async Task<T> ExecuteWithMetricsAsync<T>(
        IBookingCareMetrics metrics,
        string operation,
        string tableName,
        Func<Task<T>> databaseOperation)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await databaseOperation();
            stopwatch.Stop();
            
            // Record successful operation
            metrics.RecordDatabaseOperation(operation, tableName, stopwatch.Elapsed.TotalMilliseconds);
            metrics.IncrementOperation($"db_{operation}", "success");
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // Record failed operation
            metrics.RecordDatabaseOperation(operation, tableName, stopwatch.Elapsed.TotalMilliseconds);
            metrics.IncrementOperation($"db_{operation}", "error");
            metrics.IncrementError(ex.GetType().Name, $"db_{operation}");
            
            throw;
        }
    }

    /// <summary>
    /// Execute database operation with metrics tracking (synchronous)
    /// </summary>
    public static T ExecuteWithMetrics<T>(
        IBookingCareMetrics metrics,
        string operation,
        string tableName,
        Func<T> databaseOperation)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = databaseOperation();
            stopwatch.Stop();
            
            metrics.RecordDatabaseOperation(operation, tableName, stopwatch.Elapsed.TotalMilliseconds);
            metrics.IncrementOperation($"db_{operation}", "success");
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            metrics.RecordDatabaseOperation(operation, tableName, stopwatch.Elapsed.TotalMilliseconds);
            metrics.IncrementOperation($"db_{operation}", "error");
            metrics.IncrementError(ex.GetType().Name, $"db_{operation}");
            
            throw;
        }
    }
}

/// <summary>
/// Helper for tracking gRPC operations with metrics
/// </summary>
public static class GrpcMetricsHelper
{
    /// <summary>
    /// Execute gRPC call with automatic metrics tracking
    /// </summary>
    public static async Task<T> ExecuteGrpcCallAsync<T>(
        IBookingCareMetrics metrics,
        string methodName,
        Func<Task<T>> grpcCall)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await grpcCall();
            stopwatch.Stop();
            
            metrics.RecordGrpcOperation(methodName, "success", stopwatch.Elapsed.TotalMilliseconds);
            metrics.IncrementOperation($"grpc_{methodName}", "success");
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            metrics.RecordGrpcOperation(methodName, "error", stopwatch.Elapsed.TotalMilliseconds);
            metrics.IncrementOperation($"grpc_{methodName}", "error");
            metrics.IncrementError(ex.GetType().Name, $"grpc_{methodName}");
            
            throw;
        }
    }
}

/// <summary>
/// Helper for tracking external API calls
/// </summary>
public static class ExternalApiMetricsHelper
{
    /// <summary>
    /// Execute HTTP call with automatic metrics tracking
    /// </summary>
    public static async Task<T> ExecuteHttpCallAsync<T>(
        IBookingCareMetrics metrics,
        string apiName,
        string endpoint,
        Func<Task<HttpResponseMessage>> httpCall,
        Func<HttpResponseMessage, Task<T>> responseProcessor)
    {
        var stopwatch = Stopwatch.StartNew();
        HttpResponseMessage? response = null;
        
        try
        {
            response = await httpCall();
            var result = await responseProcessor(response);
            stopwatch.Stop();
            
            metrics.RecordExternalApiCall(
                apiName, 
                endpoint, 
                ((int)response.StatusCode).ToString(), 
                stopwatch.Elapsed.TotalMilliseconds);
                
            var resultType = response.IsSuccessStatusCode ? "success" : "error";
            metrics.IncrementOperation($"api_{apiName}", resultType);
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            var statusCode = response?.StatusCode.ToString() ?? "exception";
            metrics.RecordExternalApiCall(apiName, endpoint, statusCode, stopwatch.Elapsed.TotalMilliseconds);
            metrics.IncrementOperation($"api_{apiName}", "error");
            metrics.IncrementError(ex.GetType().Name, $"api_{apiName}");
            
            throw;
        }
    }
}