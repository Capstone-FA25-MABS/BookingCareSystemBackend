using BookingCare.Shared.Common.Models;
using BookingCare.Shared.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System.Net;

namespace BookingCare.Shared.Common.Filters;

/// <summary>
/// Global exception filter for handling exceptions in controllers
/// </summary>
public class GlobalExceptionFilter : IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;

    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        var exception = context.Exception;
        var response = exception switch
        {
            BookingCareException bookingCareEx => HandleBookingCareException(bookingCareEx),
            _ => HandleGenericException(exception)
        };

        // Log the exception
        LogException(exception, context);

        context.Result = new ObjectResult(response.ApiResponse)
        {
            StatusCode = (int)response.StatusCode
        };

        context.ExceptionHandled = true;
    }

    private ExceptionResponse HandleBookingCareException(BookingCareException exception)
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

    private ExceptionResponse HandleGenericException(Exception exception)
    {
        return new ExceptionResponse
        {
            StatusCode = HttpStatusCode.InternalServerError,
            ApiResponse = ApiResponse<object>.ErrorResult(
                "An unexpected error occurred",
                new List<string> { "INTERNAL_SERVER_ERROR" }
            )
        };
    }

    private void LogException(Exception exception, ExceptionContext context)
    {
        var request = context.HttpContext.Request;
        var userId = context.HttpContext.User?.Identity?.Name ?? "Anonymous";
        var correlationId = context.HttpContext.TraceIdentifier;
        var actionName = context.ActionDescriptor.DisplayName;

        var logLevel = exception switch
        {
            BookingCareException bookingCareEx when bookingCareEx.StatusCode == HttpStatusCode.InternalServerError => LogLevel.Error,
            BookingCareException => LogLevel.Warning,
            _ => LogLevel.Error
        };

        _logger.Log(logLevel, exception,
            "Exception in action - User: {UserId}, Action: {ActionName}, Method: {Method}, Path: {Path}, CorrelationId: {CorrelationId}, Exception: {ExceptionType}",
            userId, actionName, request.Method, request.Path, correlationId, exception.GetType().Name);

        // Log full details for internal server errors
        if (logLevel == LogLevel.Error)
        {
            _logger.LogError("Full exception details: {Exception}", exception.ToString());
        }
    }
}

/// <summary>
/// Internal class for exception response handling
/// </summary>
internal class ExceptionResponse
{
    public HttpStatusCode StatusCode { get; set; }
    public ApiResponse<object> ApiResponse { get; set; } = null!;
}
