using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BookingCare.Shared.Common.Extensions;

/// <summary>
/// Extension methods for configuring gRPC clients
/// </summary>
public static class GrpcClientExtensions
{
    /// <summary>
    /// Add gRPC client for Hospital Service
    /// </summary>
    public static IServiceCollection AddHospitalGrpcClients(this IServiceCollection services, IConfiguration configuration)
    {
        var hospitalAddress = configuration.GetSection("GrpcClients:Hospital:Address").Value ?? "http://localhost:6104";

        services.AddGrpcClient<BookingCare.Services.Hospital.HospitalService.HospitalServiceClient>(options =>
        {
            options.Address = new Uri(hospitalAddress);
        });

        services.AddGrpcClient<BookingCare.Services.Hospital.SubscriptionUsageGrpc.SubscriptionUsageGrpcClient>(options =>
        {
            options.Address = new Uri(hospitalAddress);
        });

        return services;
    }

    /// <summary>
    /// Add gRPC client for Review Service
    /// </summary>
    public static IServiceCollection AddReviewGrpcClient(this IServiceCollection services, IConfiguration configuration)
    {
        var reviewAddress = configuration.GetSection("GrpcClients:Review:Address").Value ?? "http://localhost:6112";

        services.AddGrpcClient<BookingCare.Services.Review.ReviewService.ReviewServiceClient>(options =>
        {
            options.Address = new Uri(reviewAddress);
        });

        return services;
    }
}
