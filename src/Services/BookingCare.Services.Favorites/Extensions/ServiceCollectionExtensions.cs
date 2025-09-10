using BookingCare.Services.Favorites.Data;
using BookingCare.Services.Favorites.Models.Configuration;
using BookingCare.Services.Favorites.Repositories.Implementations;
using BookingCare.Services.Favorites.Repositories.Interfaces;
using BookingCare.Services.Favorites.Services.Implementations;
using BookingCare.Services.Favorites.Services.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BookingCare.Services.Favorites.Extensions;

/// <summary>
/// Service collection extensions for dependency injection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add MongoDB services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddMongoDb(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure MongoDbSettings from appsettings.json
        services.Configure<MongoDbSettings>(
            configuration.GetSection("MongoDbSettings"));

        // Register MongoDB client
        services.AddSingleton<IMongoClient>(provider =>
        {
            var mongoDbSettings = provider.GetRequiredService<IOptions<MongoDbSettings>>().Value;

            if (string.IsNullOrEmpty(mongoDbSettings.ConnectionString))
            {
                throw new InvalidOperationException("MongoDB connection string is not configured in MongoDbSettings");
            }

            return new MongoClient(mongoDbSettings.ConnectionString);
        });

        // Register MongoDB database
        services.AddSingleton<IMongoDatabase>(provider =>
        {
            var client = provider.GetRequiredService<IMongoClient>();
            var mongoDbSettings = provider.GetRequiredService<IOptions<MongoDbSettings>>().Value;

            if (string.IsNullOrEmpty(mongoDbSettings.DatabaseName))
            {
                throw new InvalidOperationException("MongoDB database name is not configured in MongoDbSettings");
            }

            return client.GetDatabase(mongoDbSettings.DatabaseName);
        });

        // Register DbContext
        services.AddScoped<FavoritesDbContext>();

        return services;
    }

    /// <summary>
    /// Add application services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register repositories
        services.AddScoped<IFavoriteRepository, FavoriteRepository>();

        // Register services
        services.AddScoped<IFavoriteService, FavoriteService>();

        return services;
    }

    /// <summary>
    /// Add AutoMapper
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddAutoMapperServices(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(Program).Assembly);
        return services;
    }

    /// <summary>
    /// Add FluentValidation services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddValidationServices(this IServiceCollection services)
    {
        // Register all validators from assembly
        services.AddValidatorsFromAssembly(typeof(Program).Assembly);

        // Configure automatic validation behavior for better error responses
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(x => x.Value!.Errors)
                    .Select(x => x.ErrorMessage)
                    .ToList();

                var response = new
                {
                    success = false,
                    message = "Validation failed",
                    errors = errors,
                    data = (object?)null,
                    timestamp = DateTime.UtcNow
                };

                return new BadRequestObjectResult(response);
            };
        });

        return services;
    }

    /// <summary>
    /// Initialize database
    /// </summary>
    /// <param name="serviceProvider">Service provider</param>
    /// <returns>Task</returns>
    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FavoritesDbContext>();

        // Create indexes
        await dbContext.CreateIndexesAsync();
    }
}