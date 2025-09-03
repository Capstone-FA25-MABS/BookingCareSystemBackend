using MongoDB.Driver;
using BookingCare.Services.Communication.Data;
using BookingCare.Services.Communication.Data.Configuration;
using BookingCare.Services.Communication.Repositories.Interfaces;
using BookingCare.Services.Communication.Repositories.Implementations;
using BookingCare.Services.Communication.Services.Interfaces;
using BookingCare.Services.Communication.Services.Implementations;
using BookingCare.Services.Communication.Mappings;
using FluentValidation;
using FluentValidation.AspNetCore;

namespace BookingCare.Services.Communication.Extensions;

/// <summary>
/// Extension methods ?? c?u hình MongoDB services
/// </summary>
public static class MongoDbServiceExtensions
{
    /// <summary>
    /// Thêm MongoDB services vào dependency injection container
    /// </summary>
    public static IServiceCollection AddMongoDb(this IServiceCollection services, IConfiguration configuration)
    {
        // ??ng ký MongoDB configuration
        var mongoConfig = new MongoDbConfiguration();
        configuration.GetSection("MongoDB").Bind(mongoConfig);
        services.AddSingleton(mongoConfig);

        // ??ng ký MongoDB connection
        services.AddSingleton<MongoDbConnection>();
        
        // ??ng ký IMongoDatabase
        services.AddScoped<IMongoDatabase>(provider =>
        {
            var connection = provider.GetRequiredService<MongoDbConnection>();
            return connection.Database;
        });

        // ??ng ký DbContext
        services.AddScoped<CommunicationDbContext>();

        // ??ng ký repositories
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<ICallLogRepository, CallLogRepository>();

        // ??ng ký services
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<ICallLogService, CallLogService>();

        // ??ng ký AutoMapper
        services.AddAutoMapper(typeof(CommunicationMappingProfile));

        // ??ng ký FluentValidation
        services.AddFluentValidationAutoValidation();
        services.AddFluentValidationClientsideAdapters();
        services.AddValidatorsFromAssemblyContaining<CommunicationMappingProfile>();

        return services;
    }

    /// <summary>
    /// T?o indexes cho MongoDB collections
    /// </summary>
    public static async Task InitializeMongoDbAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CommunicationDbContext>();
        
        // T?o indexes
        await MongoDbIndexConfiguration.CreateIndexesAsync(context);
    }
}