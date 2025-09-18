namespace BookingCare.Services.Review.Models.DTOs;

/// <summary>
/// Standardized error response for various error types
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Indicates if the request was successful
    /// </summary>
    public bool Success { get; set; } = false;

    /// <summary>
    /// General error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// List of specific field errors
    /// </summary>
    public List<FieldError> Errors { get; set; } = new();

    /// <summary>
    /// Additional error details
    /// </summary>
    public ErrorDetails Details { get; set; } = new();
}

/// <summary>
/// Specific field error information
/// </summary>
public class FieldError
{
    /// <summary>
    /// Field name that has the error
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The value that was attempted to be set
    /// </summary>
    public string? AttemptedValue { get; set; }

    /// <summary>
    /// Error code for programmatic handling
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;
}

/// <summary>
/// Additional error context information
/// </summary>
public class ErrorDetails
{
    /// <summary>
    /// Type of error (ModelBinding, Validation, Business, etc.)
    /// </summary>
    public string ErrorType { get; set; } = string.Empty;

    /// <summary>
    /// Total number of errors
    /// </summary>
    public int TotalErrors { get; set; }

    /// <summary>
    /// When the error occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Helpful suggestion for fixing the error
    /// </summary>
    public string? Suggestion { get; set; }

    /// <summary>
    /// API documentation URL for reference
    /// </summary>
    public string? DocumentationUrl { get; set; }
}

/// <summary>
/// Helper class for creating consistent error responses
/// </summary>
public static class ErrorResponseFactory
{
    /// <summary>
    /// Creates a model binding error response
    /// </summary>
    public static ErrorResponse CreateModelBindingError(List<FieldError> fieldErrors)
    {
        return new ErrorResponse
        {
            Success = false,
            Message = "Invalid request format - please check field data types",
            Errors = fieldErrors,
            Details = new ErrorDetails
            {
                ErrorType = "ModelBindingError",
                TotalErrors = fieldErrors.Count,
                Suggestion = "Verify that all fields have the correct data types as specified in the API documentation",
                DocumentationUrl = "/swagger"
            }
        };
    }

    /// <summary>
    /// Creates a validation error response
    /// </summary>
    public static ErrorResponse CreateValidationError(List<FieldError> fieldErrors, string validatedObjectName)
    {
        return new ErrorResponse
        {
            Success = false,
            Message = "Validation failed - please correct the following errors",
            Errors = fieldErrors,
            Details = new ErrorDetails
            {
                ErrorType = "ValidationError",
                TotalErrors = fieldErrors.Count,
                Suggestion = $"Please review the validation rules for {validatedObjectName} and correct the invalid fields",
                DocumentationUrl = "/swagger"
            }
        };
    }

    /// <summary>
    /// Creates a business logic error response
    /// </summary>
    public static ErrorResponse CreateBusinessError(string message, string errorCode = "BusinessLogicError")
    {
        return new ErrorResponse
        {
            Success = false,
            Message = message,
            Errors = new List<FieldError>(),
            Details = new ErrorDetails
            {
                ErrorType = errorCode,
                TotalErrors = 0,
                Suggestion = "Please review the business rules and try again"
            }
        };
    }

    /// <summary>
    /// Creates a null request error response
    /// </summary>
    public static ErrorResponse CreateNullRequestError(string parameterName)
    {
        return new ErrorResponse
        {
            Success = false,
            Message = "Request data is missing or invalid",
            Errors = new List<FieldError>
            {
                new FieldError
                {
                    Field = parameterName,
                    Message = $"Request parameter '{parameterName}' is null. This usually indicates invalid JSON format or data type mismatch.",
                    AttemptedValue = "null",
                    ErrorCode = "NullRequestParameter"
                }
            },
            Details = new ErrorDetails
            {
                ErrorType = "NullRequestParameter",
                TotalErrors = 1,
                Suggestion = "Please check your JSON format and ensure all required fields have correct data types",
                DocumentationUrl = "/swagger"
            }
        };
    }
}