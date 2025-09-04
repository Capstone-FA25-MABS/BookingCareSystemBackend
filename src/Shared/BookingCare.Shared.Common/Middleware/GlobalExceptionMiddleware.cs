using BookingCare.Shared.Common.Models;
using BookingCare.Shared.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using System.Diagnostics;

namespace BookingCare.Shared.Common.Middleware;

/// <summary>
/// Global exception handling middleware that catches all unhandled exceptions
/// and returns consistent error responses across the application.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        LogException(exception, context);

        // Determine status code first
        var statusCode = exception switch
        {
            BookingCareException bookingCareException => bookingCareException.StatusCode,
            TaskCanceledException => HttpStatusCode.RequestTimeout,
            OperationCanceledException => HttpStatusCode.RequestTimeout,
            _ => HttpStatusCode.InternalServerError
        };

        // Add telemetry for monitoring
        var activity = Activity.Current;
        if (activity != null)
        {
            activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            activity.SetTag("exception.type", exception.GetType().Name);
            activity.SetTag("exception.message", exception.Message);
            activity.SetTag("http.status_code", ((int)statusCode).ToString());
        }

        // Record error metrics if monitoring extensions are available
        try
        {
            var errorType = exception switch
            {
                BookingCareException => "business_error",
                TaskCanceledException => "timeout",
                OperationCanceledException => "cancelled",
                _ => "server_error"
            };
            
            // This will only work if MonitoringExtensions is available
            var monitoringType = Type.GetType("BookingCare.Shared.Common.Extensions.MonitoringExtensions");
            if (monitoringType != null)
            {
                var recordErrorMethod = monitoringType.GetMethod("RecordError");
                recordErrorMethod?.Invoke(null, new object[] { "api", errorType });
            }
        }
        catch
        {
            // Ignore if monitoring is not available
        }

        var response = exception switch
        {
            BookingCareException bookingCareException => HandleBookingCareException(bookingCareException),
            TaskCanceledException => HandleTaskCancelledException(),
            OperationCanceledException => HandleOperationCancelledException(),
            _ => HandleGenericException(exception)
        };

        context.Response.StatusCode = (int)response.StatusCode;
        context.Response.ContentType = "application/json";

        var jsonResponse = JsonSerializer.Serialize(response.ApiResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    private static ExceptionResponse HandleBookingCareException(BookingCareException exception)
    {
        List<string>? errors = null;
        
        // Add validation errors if available
        if (exception is ValidationException validationEx && validationEx.ValidationErrors.Any())
        {
            errors = validationEx.ValidationErrors.Select(ve => $"{ve.Field}: {ve.Message}").ToList();
        }

        return new ExceptionResponse
        {
            StatusCode = exception.StatusCode,
            ApiResponse = ApiResponse<object>.ErrorResult(
                exception.Message,
                errors
            )
        };
    }

    private static ExceptionResponse HandleTaskCancelledException()
    {
        return new ExceptionResponse
        {
            StatusCode = HttpStatusCode.RequestTimeout,
            ApiResponse = ApiResponse<object>.ErrorResult(
                "The request was cancelled due to timeout",
                null
            )
        };
    }

    private static ExceptionResponse HandleOperationCancelledException()
    {
        return new ExceptionResponse
        {
            StatusCode = HttpStatusCode.RequestTimeout,
            ApiResponse = ApiResponse<object>.ErrorResult(
                "The operation was cancelled",
                null
            )
        };
    }

    private static ExceptionResponse HandleGenericException(Exception exception)
    {
        return new ExceptionResponse
        {
            StatusCode = HttpStatusCode.InternalServerError,
            ApiResponse = ApiResponse<object>.ErrorResult(
                "An unexpected error occurred. Please try again later.",
                null
            )
        };
    }

    private void LogException(Exception exception, HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();
        
        switch (exception)
        {
            case BookingCareException bookingCareException:
                _logger.LogWarning(bookingCareException,
                    "BookingCare Exception occurred. CorrelationId: {CorrelationId}, StatusCode: {StatusCode}, Message: {Message}",
                    correlationId, bookingCareException.StatusCode, bookingCareException.Message);
                break;
                
            case TaskCanceledException:
                _logger.LogInformation("Request was cancelled. CorrelationId: {CorrelationId}, Path: {Path}",
                    correlationId, context.Request.Path);
                break;
                
            case OperationCanceledException:
                _logger.LogInformation("Operation was cancelled. CorrelationId: {CorrelationId}, Path: {Path}",
                    correlationId, context.Request.Path);
                break;
                
            default:
                _logger.LogError(exception,
                    "Unhandled exception occurred. CorrelationId: {CorrelationId}, Path: {Path}, Method: {Method}",
                    correlationId, context.Request.Path, context.Request.Method);
                break;
        }
    }
}

/// <summary>
/// Helper class for exception response handling
/// </summary>
internal class ExceptionResponse
{
    public HttpStatusCode StatusCode { get; set; }
    public ApiResponse<object> ApiResponse { get; set; } = null!;
}
