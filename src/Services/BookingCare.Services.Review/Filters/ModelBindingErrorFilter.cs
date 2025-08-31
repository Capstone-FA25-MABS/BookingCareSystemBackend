using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BookingCare.Services.Review.Filters;

/// <summary>
/// Filter to handle model binding errors and convert them to standardized error responses
/// </summary>
public class ModelBindingErrorFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = new List<object>();
            
            foreach (var modelError in context.ModelState)
            {
                var fieldName = modelError.Key;
                var fieldErrors = modelError.Value.Errors;
                
                foreach (var error in fieldErrors)
                {
                    // Check if it's a conversion error (like invalid GUID format)
                    var errorMessage = GetFriendlyErrorMessage(fieldName, error, modelError.Value.AttemptedValue);
                    
                    errors.Add(new
                    {
                        field = fieldName,
                        message = errorMessage,
                        attemptedValue = modelError.Value.AttemptedValue,
                        errorCode = "ModelBindingError"
                    });
                }
            }

            var errorResponse = new
            {
                success = false,
                message = "Invalid request format",
                errors = errors,
                details = new
                {
                    errorType = "ModelBindingError",
                    totalErrors = errors.Count,
                    timestamp = DateTime.UtcNow,
                    suggestion = "Please check the data types and format of your request fields"
                }
            };

            context.Result = new BadRequestObjectResult(errorResponse);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // No action needed after execution
    }

    private static string GetFriendlyErrorMessage(string fieldName, ModelError error, string? attemptedValue)
    {
        var errorMessage = error.ErrorMessage;
        
        // Handle common GUID conversion errors
        if (errorMessage.Contains("is not a valid value for Guid") || 
            errorMessage.Contains("Unable to convert") ||
            errorMessage.Contains("The value") && errorMessage.Contains("is not valid"))
        {
            if (fieldName.ToLower().Contains("id"))
            {
                return $"'{fieldName}' must be a valid GUID format (e.g., '550e8400-e29b-41d4-a716-446655440000'). Received: '{attemptedValue}'";
            }
        }
        
        // Handle enum conversion errors
        if (errorMessage.Contains("The value") && errorMessage.Contains("is not valid for"))
        {
            if (fieldName.ToLower().Contains("targettype"))
            {
                return $"'{fieldName}' must be either 'DOCTOR' or 'SERVICE'. Received: '{attemptedValue}'";
            }
        }
        
        // Handle integer conversion errors  
        if (errorMessage.Contains("is not a valid value for Int32"))
        {
            if (fieldName.ToLower().Contains("rating"))
            {
                return $"'{fieldName}' must be a valid integer between 1 and 5. Received: '{attemptedValue}'";
            }
            
            if (fieldName.ToLower().Contains("page"))
            {
                return $"'{fieldName}' must be a valid integer greater than 0. Received: '{attemptedValue}'";
            }
        }
        
        // Handle DateTime conversion errors
        if (errorMessage.Contains("is not a valid value for DateTime"))
        {
            return $"'{fieldName}' must be a valid date format (ISO 8601: YYYY-MM-DDTHH:mm:ss.sssZ). Received: '{attemptedValue}'";
        }

        // Handle boolean conversion errors
        if (errorMessage.Contains("is not a valid value for Boolean"))
        {
            return $"'{fieldName}' must be either 'true' or 'false'. Received: '{attemptedValue}'";
        }

        // Generic fallback
        return $"'{fieldName}' has invalid format. Received: '{attemptedValue}'. {errorMessage}";
    }
}

/// <summary>
/// Extension methods for adding model binding error filter
/// </summary>
public static class ModelBindingErrorFilterExtensions
{
    /// <summary>
    /// Add model binding error filter to handle conversion errors
    /// </summary>
    public static IServiceCollection AddModelBindingErrorFilter(this IServiceCollection services)
    {
        services.AddScoped<ModelBindingErrorFilter>();
        
        services.Configure<MvcOptions>(options =>
        {
            options.Filters.Add<ModelBindingErrorFilter>();
        });

        return services;
    }
}