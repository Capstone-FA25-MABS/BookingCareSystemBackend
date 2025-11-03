using BookingCare.Services.Hospital.Repositories.Interfaces;
using BookingCare.Services.Hospital.Repositories.Implementations;
using BookingCare.Services.Hospital.Services.Interfaces;
using BookingCare.Services.Hospital.Services.Implementations;
using BookingCare.Services.Hospital.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BookingCare.Services.Hospital.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHospitalServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<HospitalDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddScoped<IHospitalRepository, HospitalRepository>();
        services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
        services.AddScoped<IHospitalSubscriptionRepository, HospitalSubscriptionRepository>();
        // services.AddScoped<IHospitalSpecialtyRepository, HospitalSpecialtyRepository>();
        // services.AddScoped<IHospitalImageRepository, HospitalImageRepository>();

        // Services
        // services.AddScoped<IHospitalService, HospitalService>(); // Already registered in Program.cs
        services.AddScoped<ISubscriptionPlanService, SubscriptionPlanService>();
        services.AddScoped<IHospitalSubscriptionService, HospitalSubscriptionService>();
        services.AddScoped<ISubscriptionUsageService, SubscriptionUsageService>();

        // Background Services
        services.AddHostedService<SubscriptionRenewalService>();

        return services;
    }
}
