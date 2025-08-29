using Microsoft.Extensions.Logging;

namespace BookingCare.Shared.Common.Services;

/// <summary>
/// Base service class with common functionality for all services
/// </summary>
public abstract class BaseService
{
    protected readonly ILogger Logger;

    protected BaseService(ILogger logger)
    {
        Logger = logger;
    }

    /// <summary>
    /// Logs an information message with correlation ID
    /// </summary>
    /// <param name="message">The message to log</param>
    /// <param name="correlationId">Optional correlation ID</param>
    /// <param name="args">Optional arguments for the message</param>
    protected void LogInfo(string message, string? correlationId = null, params object[] args)
    {
        if (!string.IsNullOrEmpty(correlationId))
        {
            Logger.LogInformation($"[{correlationId}] {message}", args);
        }
        else
        {
            Logger.LogInformation(message, args);
        }
    }

    /// <summary>
    /// Logs a warning message with correlation ID
    /// </summary>
    /// <param name="message">The message to log</param>
    /// <param name="correlationId">Optional correlation ID</param>
    /// <param name="args">Optional arguments for the message</param>
    protected void LogWarning(string message, string? correlationId = null, params object[] args)
    {
        if (!string.IsNullOrEmpty(correlationId))
        {
            Logger.LogWarning($"[{correlationId}] {message}", args);
        }
        else
        {
            Logger.LogWarning(message, args);
        }
    }

    /// <summary>
    /// Logs an error message with correlation ID
    /// </summary>
    /// <param name="exception">The exception to log</param>
    /// <param name="message">The message to log</param>
    /// <param name="correlationId">Optional correlation ID</param>
    /// <param name="args">Optional arguments for the message</param>
    protected void LogError(Exception exception, string message, string? correlationId = null, params object[] args)
    {
        if (!string.IsNullOrEmpty(correlationId))
        {
            Logger.LogError(exception, $"[{correlationId}] {message}", args);
        }
        else
        {
            Logger.LogError(exception, message, args);
        }
    }

    /// <summary>
    /// Logs a debug message with correlation ID
    /// </summary>
    /// <param name="message">The message to log</param>
    /// <param name="correlationId">Optional correlation ID</param>
    /// <param name="args">Optional arguments for the message</param>
    protected void LogDebug(string message, string? correlationId = null, params object[] args)
    {
        if (!string.IsNullOrEmpty(correlationId))
        {
            Logger.LogDebug($"[{correlationId}] {message}", args);
        }
        else
        {
            Logger.LogDebug(message, args);
        }
    }

    /// <summary>
    /// Validates that a required parameter is not null
    /// </summary>
    /// <typeparam name="T">The type of the parameter</typeparam>
    /// <param name="value">The value to validate</param>
    /// <param name="parameterName">The name of the parameter</param>
    /// <exception cref="ArgumentNullException">Thrown when the value is null</exception>
    protected static void ValidateRequired<T>(T? value, string parameterName) where T : class
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }
    }

    /// <summary>
    /// Validates that a required string parameter is not null or empty
    /// </summary>
    /// <param name="value">The value to validate</param>
    /// <param name="parameterName">The name of the parameter</param>
    /// <exception cref="ArgumentException">Thrown when the value is null or empty</exception>
    protected static void ValidateRequiredString(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or empty", parameterName);
        }
    }

    /// <summary>
    /// Validates that a GUID parameter is not empty
    /// </summary>
    /// <param name="value">The GUID to validate</param>
    /// <param name="parameterName">The name of the parameter</param>
    /// <exception cref="ArgumentException">Thrown when the GUID is empty</exception>
    protected static void ValidateGuid(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("GUID cannot be empty", parameterName);
        }
    }

    /// <summary>
    /// Executes an action and handles exceptions with logging
    /// </summary>
    /// <param name="action">The action to execute</param>
    /// <param name="operation">The name of the operation for logging</param>
    /// <param name="correlationId">Optional correlation ID</param>
    /// <returns>A task representing the operation</returns>
    protected async Task ExecuteWithErrorHandling(Func<Task> action, string operation, string? correlationId = null)
    {
        try
        {
            LogDebug($"Starting operation: {operation}", correlationId);
            await action();
            LogDebug($"Completed operation: {operation}", correlationId);
        }
        catch (Exception ex)
        {
            LogError(ex, $"Error in operation: {operation}", correlationId);
            throw;
        }
    }

    /// <summary>
    /// Executes a function and handles exceptions with logging
    /// </summary>
    /// <typeparam name="T">The return type of the function</typeparam>
    /// <param name="function">The function to execute</param>
    /// <param name="operation">The name of the operation for logging</param>
    /// <param name="correlationId">Optional correlation ID</param>
    /// <returns>A task representing the operation with a result</returns>
    protected async Task<T> ExecuteWithErrorHandling<T>(Func<Task<T>> function, string operation, string? correlationId = null)
    {
        try
        {
            LogDebug($"Starting operation: {operation}", correlationId);
            var result = await function();
            LogDebug($"Completed operation: {operation}", correlationId);
            return result;
        }
        catch (Exception ex)
        {
            LogError(ex, $"Error in operation: {operation}", correlationId);
            throw;
        }
    }
}
