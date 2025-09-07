using BookingCare.Shared.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace BookingCare.Shared.Common.Extensions;

/// <summary>
/// Extensions for configuring validation behavior across all services
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Configures custom model validation behavior to return consistent API response format
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCustomValidation(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                // Extract validation errors from ModelState
                var validationErrors = new List<string>();

                foreach (var (key, value) in context.ModelState)
                {
                    if (value?.Errors.Count > 0)
                    {
                        foreach (var error in value.Errors)
                        {
                            // Format error messages consistently
                            var fieldName = string.IsNullOrEmpty(key) ? "" : $"{key}: ";
                            var errorMessage = !string.IsNullOrEmpty(error.ErrorMessage)
                                ? error.ErrorMessage
                                : error.Exception?.Message ?? "Invalid value";

                            // Remove field name prefix if it's already in the error message
                            if (!string.IsNullOrEmpty(key) && errorMessage.StartsWith($"The {key}", StringComparison.OrdinalIgnoreCase))
                            {
                                validationErrors.Add(errorMessage);
                            }
                            else if (!string.IsNullOrEmpty(key))
                            {
                                validationErrors.Add($"{fieldName}{errorMessage}");
                            }
                            else
                            {
                                validationErrors.Add(errorMessage);
                            }
                        }
                    }
                }

                // Create consistent error response using ApiResponse format
                var apiResponse = ApiResponse<object>.ErrorResult(
                    message: "One or more validation errors occurred.",
                    errors: validationErrors
                );

                return new BadRequestObjectResult(apiResponse);
            };
        });

        return services;
    }
}
