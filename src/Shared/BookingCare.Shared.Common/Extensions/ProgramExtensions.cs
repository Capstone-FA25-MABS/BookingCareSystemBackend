using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace BookingCare.Shared.Common.Extensions;

/// <summary>
/// Extension methods for configuring common services in Program.cs files
/// </summary>
public static class ProgramExtensions
{
    /// <summary>
    /// Adds common controller configuration with JSON enum conversion
    /// </summary>
    public static IServiceCollection AddCommonControllers(this IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                // Handle reference loops to prevent circular reference errors
                options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.MaxDepth = 32;
            });
        return services;
    }

    /// <summary>
    /// Adds common Swagger configuration
    /// </summary>
    public static IServiceCollection AddCommonSwagger(this IServiceCollection services, string serviceName)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            // Register common group names to avoid mismatch (some setups produce v1 instead of v1.0)
            c.SwaggerDoc("v1", new() { Title = $"BookingCare {serviceName} API", Version = "v1" });
            c.SwaggerDoc("v1.0", new() { Title = $"BookingCare {serviceName} API", Version = "v1.0" });
            // Ensure endpoints are included in the correct Swagger doc based on ApiExplorer group name (e.g., v1.0)
            c.DocInclusionPredicate((docName, apiDesc) =>
                string.Equals(docName, apiDesc.GroupName, StringComparison.OrdinalIgnoreCase));
        });
        return services;
    }

    /// <summary>
    /// Adds common logging configuration
    /// </summary>
    public static ILoggingBuilder AddCommonLogging(this ILoggingBuilder logging)
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.AddDebug();
        return logging;
    }

    /// <summary>
    /// Configures common Swagger UI for development
    /// </summary>
    public static void UseCommonSwaggerUI(this WebApplication app, string serviceName)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
            app.UseSwaggerUI(c =>
            {
                provider.ApiVersionDescriptions.ToList().ForEach(description =>
                    c.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
                        $"BookingCare {serviceName} API {description.GroupName.ToUpperInvariant()}"));
                c.RoutePrefix = "swagger";
            });
        }
    }

    /// <summary>
    /// Adds common health check endpoint
    /// </summary>
    public static void MapCommonHealthCheck(this WebApplication app, string serviceName)
    {
        app.MapGet("/health", () => Microsoft.AspNetCore.Http.Results.Ok(new
        {
            Service = serviceName,
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        }));
    }
}
