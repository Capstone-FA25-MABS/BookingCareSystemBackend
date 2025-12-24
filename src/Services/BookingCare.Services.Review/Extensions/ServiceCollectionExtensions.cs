using BookingCare.Services.Review.Data;
using BookingCare.Services.Review.Extensions;
using BookingCare.Services.Review.Repositories.Interfaces;
using BookingCare.Services.Review.Repositories.Implementations;
using BookingCare.Services.Review.Services.Interfaces;
using BookingCare.Services.Review.Services.Implementations;

namespace BookingCare.Services.Review.Extensions;

/// <summary>
/// Extension methods for service registration
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Review service dependencies to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddReviewServices(this IServiceCollection services)
    {
        // Register database context
        services.AddScoped<IReviewDbContext, ReviewDbContext>();

        // Register repositories
        services.AddScoped<IReviewRepository, ReviewRepository>();

        // Register enrichment services (optimized approach)
        services.AddScoped<IUserEnrichmentService, UserEnrichmentService>();
        services.AddScoped<IReplyEnrichmentService, ReplyEnrichmentService>();

        // Register appointment validation service
        services.AddScoped<IAppointmentValidationService, AppointmentValidationService>();

        // Keep old service for backward compatibility if needed
        services.AddScoped<IAccountEnrichmentService, AccountEnrichmentService>();

        // Register business services
        services.AddScoped<IReviewService, ReviewService>();

        return services;
    }
}