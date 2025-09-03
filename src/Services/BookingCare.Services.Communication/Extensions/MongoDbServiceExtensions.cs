using BookingCare.Services.Communication.Configuration;
using BookingCare.Services.Communication.Data;
using BookingCare.Services.Communication.Data.Configuration;
using BookingCare.Services.Communication.Mappings;
using BookingCare.Services.Communication.Models.Entities;
using BookingCare.Services.Communication.Repositories.Implementations;
using BookingCare.Services.Communication.Repositories.Interfaces;
using BookingCare.Services.Communication.Services.Implementations;
using BookingCare.Services.Communication.Services.Interfaces;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BookingCare.Services.Communication.Extensions;

/// <summary>
/// Extension methods cho MongoDB configuration
/// </summary>
public static class MongoDbServiceExtensions
{
    /// <summary>
    /// Thêm MongoDB services vào DI container
    /// </summary>
    public static IServiceCollection AddMongoDb(this IServiceCollection services, IConfiguration configuration)
    {
        // L?y connection string t? configuration
        var connectionString = configuration.GetConnectionString("MongoDB") 
            ?? configuration["MongoDB:ConnectionString"];
        
        var databaseName = configuration["MongoDB:DatabaseName"];

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("MongoDB connection string is not configured");
        }

        if (string.IsNullOrEmpty(databaseName))
        {
            throw new InvalidOperationException("MongoDB database name is not configured");
        }

        // ??ng ký MongoDB client
        services.AddSingleton<IMongoClient>(serviceProvider =>
        {
            return new MongoClient(connectionString);
        });

        // ??ng ký MongoDB database
        services.AddScoped<IMongoDatabase>(serviceProvider =>
        {
            var client = serviceProvider.GetRequiredService<IMongoClient>();
            return client.GetDatabase(databaseName);
        });
        services.AddAutoMapper(typeof(CommunicationMappingProfile));
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


        
        // ??ng ký File Upload services v?i Cloudinary
        services.AddScoped<IFileUploadService, FileUploadService>();
        services.AddScoped<ICloudStorageProvider, CloudinaryStorageProvider>();

        // ??ng ký File Upload Configuration
        services.Configure<FileUploadConfiguration>(configuration.GetSection(FileUploadConfiguration.SectionName));
        // ??ng k� FluentValidation
        services.AddFluentValidationAutoValidation();
        services.AddFluentValidationClientsideAdapters();
        services.AddValidatorsFromAssemblyContaining<CommunicationMappingProfile>();
        return services;
    }


    public static async Task InitializeMongoDbAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CommunicationDbContext>();

        // T?o indexes
        await MongoDbIndexConfiguration.CreateIndexesAsync(context);
    }
}