using BookingCare.Shared.Common.CircuitBreaker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BookingCare.Shared.Common.Extensions
{
    public static class CircuitBreakerExtensions
    {
        public static IServiceCollection AddCircuitBreaker(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Configure circuit breaker options
            services.Configure<ServiceCircuitBreakerOptions>(
                configuration.GetSection("CircuitBreaker"));

            // Register circuit breaker services
            services.AddSingleton<ICircuitBreakerService, CircuitBreakerService>();
            services.AddSingleton<IDatabaseCircuitBreakerService, DatabaseCircuitBreakerService>();
            services.AddSingleton<IHttpClientCircuitBreakerService, HttpClientCircuitBreakerService>();
            services.AddSingleton<IExternalApiCircuitBreakerService, ExternalApiCircuitBreakerService>();

            return services;
        }

        public static IServiceCollection AddCircuitBreaker(
            this IServiceCollection services,
            Action<ServiceCircuitBreakerOptions> configureOptions)
        {
            // Configure circuit breaker options
            services.Configure(configureOptions);

            // Register circuit breaker services
            services.AddSingleton<ICircuitBreakerService, CircuitBreakerService>();
            services.AddSingleton<IDatabaseCircuitBreakerService, DatabaseCircuitBreakerService>();
            services.AddSingleton<IHttpClientCircuitBreakerService, HttpClientCircuitBreakerService>();
            services.AddSingleton<IExternalApiCircuitBreakerService, ExternalApiCircuitBreakerService>();

            return services;
        }
    }
}
