using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BookingCare.Services.Review.Filters;

/// <summary>
/// Automatic validation filter using FluentValidation
/// </summary>
public class ValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Get all arguments from the action
        foreach (var argument in context.ActionArguments)
        {
            if (argument.Value == null) 
            {
                // If argument is null, it might be due to model binding failure
                // This should be handled by ModelBindingErrorFilter, but add safety check
                var argumentName = argument.Key;
                
                var errorResponse = new
                {
                    success = false,
                    message = "Request data is missing or invalid",
                    errors = new[]
                    {
                        new
                        {
                            field = argumentName,
                            message = $"Request parameter '{argumentName}' is null. This usually indicates invalid JSON format or data type mismatch.",
                            attemptedValue = "null",
                            errorCode = "NullRequestParameter"
                        }
                    },
                    details = new
                    {
                        errorType = "NullRequestParameter",
                        suggestion = "Please check your JSON format and ensure all required fields have correct data types",
                        timestamp = DateTime.UtcNow
                    }
                };

                context.Result = new BadRequestObjectResult(errorResponse);
                return;
            }

            var argumentType = argument.Value.GetType();
            
            // Try to get validator for this type
            var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);
            var validator = _serviceProvider.GetService(validatorType) as IValidator;

            if (validator != null)
            {
                // Validate the argument
                var validationResult = await validator.ValidateAsync(new ValidationContext<object>(argument.Value));

                if (!validationResult.IsValid)
                {
                    // Create standardized error response
                    var errorResponse = new
                    {
                        success = false,
                        message = "Validation failed",
                        errors = validationResult.Errors.Select(e => new
                        {
                            field = e.PropertyName,
                            message = e.ErrorMessage,
                            attemptedValue = e.AttemptedValue?.ToString(),
                            errorCode = e.ErrorCode
                        }).ToList(),
                        details = new
                        {
                            validatedObject = argumentType.Name,
                            totalErrors = validationResult.Errors.Count,
                            timestamp = DateTime.UtcNow
                        }
                    };

                    context.Result = new BadRequestObjectResult(errorResponse);
                    return;
                }
            }
        }

        // If validation passes, continue to action
        await next();
    }
}

/// <summary>
/// Extension methods for adding validation filter
/// </summary>
public static class ValidationFilterExtensions
{
    /// <summary>
    /// Add automatic validation filter to controllers
    /// </summary>
    public static IServiceCollection AddValidationFilter(this IServiceCollection services)
    {
        services.AddScoped<ValidationFilter>();
        
        services.Configure<MvcOptions>(options =>
        {
            options.Filters.Add<ValidationFilter>();
        });

        return services;
    }
}