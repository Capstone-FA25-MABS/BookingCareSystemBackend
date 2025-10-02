using BookingCare.Shared.Common.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace BookingCare.Shared.Common.Extensions;

public static class KestrelConfigurationExtensions
{
    /// <summary>
    /// Configures Kestrel with security best practices for microservices
    /// </summary>
    /// <param name="webHostBuilder">The web host builder</param>
    /// <param name="configuration">Configuration instance</param>
    /// <param name="environment">Host environment</param>
    /// <param name="serviceName">Name of the service for default port calculation</param>
    /// <returns>Configured web host builder</returns>
    public static IWebHostBuilder ConfigureSecureKestrel(
        this IWebHostBuilder webHostBuilder,
        IConfiguration configuration,
        IHostEnvironment environment,
        string serviceName = "default")
    {
        return webHostBuilder.ConfigureKestrel(options =>
        {
            var kestrelConfig = configuration.GetSection("Kestrel").Get<KestrelConfiguration>()
                ?? new KestrelConfiguration();

            EnableHttp2WithoutTlsForDevelopment(environment);
            ConfigureHttpEndpoint(options, kestrelConfig, environment, serviceName);
            ConfigureGrpcEndpoint(options, kestrelConfig, environment, serviceName);
            ConfigureConnectionLimits(options, kestrelConfig);
        });
    }

    /// <summary>
    /// Enables HTTP/2 without TLS for gRPC in development environment
    /// </summary>
    private static void EnableHttp2WithoutTlsForDevelopment(IHostEnvironment environment)
    {
        if (!environment.IsProduction())
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        }
    }

    /// <summary>
    /// Configures HTTP endpoint for REST API
    /// </summary>
    private static void ConfigureHttpEndpoint(
        KestrelServerOptions options,
        KestrelConfiguration kestrelConfig,
        IHostEnvironment environment,
        string serviceName)
    {
        var httpPort = kestrelConfig.HttpPort ?? GetDefaultHttpPort(serviceName);
        options.ListenAnyIP(httpPort, listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
            ConfigureHttpsForProduction(listenOptions, kestrelConfig, environment);
        });
    }

    /// <summary>
    /// Configures gRPC endpoint if enabled
    /// </summary>
    private static void ConfigureGrpcEndpoint(
        KestrelServerOptions options,
        KestrelConfiguration kestrelConfig,
        IHostEnvironment environment,
        string serviceName)
    {
        if (!kestrelConfig.EnableGrpc)
        {
            return;
        }

        var grpcPort = kestrelConfig.GrpcPort ?? GetDefaultGrpcPort(serviceName);
        options.ListenAnyIP(grpcPort, listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http2;
            ConfigureHttpsForProduction(listenOptions, kestrelConfig, environment);
        });
    }

    /// <summary>
    /// Configures HTTPS for production environment
    /// </summary>
    private static void ConfigureHttpsForProduction(
        ListenOptions listenOptions,
        KestrelConfiguration kestrelConfig,
        IHostEnvironment environment)
    {
        if (!environment.IsProduction())
        {
            return;
        }

        if (HasCustomCertificate(kestrelConfig))
        {
            listenOptions.UseHttps(kestrelConfig.CertificatePath!, kestrelConfig.CertificatePassword!);
        }
        else
        {
            listenOptions.UseHttps(); // Use default certificate
        }
    }

    /// <summary>
    /// Checks if custom certificate is configured
    /// </summary>
    private static bool HasCustomCertificate(KestrelConfiguration kestrelConfig)
    {
        return !string.IsNullOrEmpty(kestrelConfig.CertificatePath) &&
               !string.IsNullOrEmpty(kestrelConfig.CertificatePassword);
    }

    /// <summary>
    /// Configures connection limits for security
    /// </summary>
    private static void ConfigureConnectionLimits(
        KestrelServerOptions options,
        KestrelConfiguration kestrelConfig)
    {
        options.Limits.MaxConcurrentConnections = kestrelConfig.MaxConcurrentConnections;
        options.Limits.MaxConcurrentUpgradedConnections = kestrelConfig.MaxConcurrentUpgradedConnections;
        options.Limits.MaxRequestBodySize = kestrelConfig.MaxRequestBodySize;
        options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(kestrelConfig.RequestHeadersTimeoutSeconds);
        options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(kestrelConfig.KeepAliveTimeoutSeconds);
    }

    /// <summary>
    /// Gets default HTTP port based on service name
    /// </summary>
    private static int GetDefaultHttpPort(string serviceName)
    {
        return serviceName.ToLowerInvariant() switch
        {
            "ai" => 6000,
            "analytics" => 6001,
            "appointment" => 6002,
            "auth" => 6003,
            "clinic" => 6004,
            "communication" => 6005,
            "content" => 6006,
            "discount" => 6007,
            "doctor" => 6008,
            "favorites" => 6009,
            "hospital" => 6010,
            "notification" => 6011,
            "payment" => 6012,
            "review" => 6013,
            "user" => 6014,
            "saga" => 6015,
            "schedule" => 6016,
            "servicemedical" => 6017,
            "gateway" => 5000,
            _ => 6000
        };
    }

    /// <summary>
    /// Gets default gRPC port based on service name
    /// </summary>
    private static int GetDefaultGrpcPort(string serviceName)
    {
        return serviceName.ToLowerInvariant() switch
        {
            "ai" => 6100,
            "analytics" => 6101,
            "appointment" => 6102,
            "auth" => 6103,
            "clinic" => 6104,
            "communication" => 6105,
            "content" => 6106,
            "discount" => 6107,
            "doctor" => 6108,
            "favorites" => 6109,
            "hospital" => 6110,
            "notification" => 6111,
            "payment" => 6112,
            "review" => 6113,
            "user" => 6114,
            "saga" => 6115,
            "schedule" => 6116,
            "servicemedical" => 6117,
            _ => 6100
        };
    }
}