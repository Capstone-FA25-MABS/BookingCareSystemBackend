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

            // Enable HTTP/2 without TLS for gRPC in development only
            if (!environment.IsProduction())
            {
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            }

            // Configure HTTP endpoint for REST API
            var httpPort = kestrelConfig.HttpPort ?? GetDefaultHttpPort(serviceName);
            options.ListenAnyIP(httpPort, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http1AndHttp2;

                // Enable HTTPS in production
                if (environment.IsProduction())
                {
                    if (!string.IsNullOrEmpty(kestrelConfig.CertificatePath) &&
                        !string.IsNullOrEmpty(kestrelConfig.CertificatePassword))
                    {
                        listenOptions.UseHttps(kestrelConfig.CertificatePath, kestrelConfig.CertificatePassword);
                    }
                    else
                    {
                        listenOptions.UseHttps(); // Use default certificate
                    }
                }
            });

            // Configure gRPC endpoint if enabled
            if (kestrelConfig.EnableGrpc)
            {
                var grpcPort = kestrelConfig.GrpcPort ?? GetDefaultGrpcPort(serviceName);
                options.ListenAnyIP(grpcPort, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http2;

                    // Enable HTTPS in production
                    if (environment.IsProduction())
                    {
                        if (!string.IsNullOrEmpty(kestrelConfig.CertificatePath) &&
                            !string.IsNullOrEmpty(kestrelConfig.CertificatePassword))
                        {
                            listenOptions.UseHttps(kestrelConfig.CertificatePath, kestrelConfig.CertificatePassword);
                        }
                        else
                        {
                            listenOptions.UseHttps(); // Use default certificate
                        }
                    }
                });
            }

            // Configure connection limits for security
            options.Limits.MaxConcurrentConnections = kestrelConfig.MaxConcurrentConnections;
            options.Limits.MaxConcurrentUpgradedConnections = kestrelConfig.MaxConcurrentUpgradedConnections;
            options.Limits.MaxRequestBodySize = kestrelConfig.MaxRequestBodySize;
            options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(kestrelConfig.RequestHeadersTimeoutSeconds);
            options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(kestrelConfig.KeepAliveTimeoutSeconds);
        });
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

/// <summary>
/// Configuration model for Kestrel settings
/// </summary>
public class KestrelConfiguration
{
    /// <summary>
    /// HTTP port for REST API (optional, will use service-based default if not set)
    /// </summary>
    public int? HttpPort { get; set; }

    /// <summary>
    /// gRPC port (optional, will use service-based default if not set)
    /// </summary>
    public int? GrpcPort { get; set; }

    /// <summary>
    /// Whether to enable gRPC endpoint
    /// </summary>
    public bool EnableGrpc { get; set; } = true;

    /// <summary>
    /// Path to SSL certificate for HTTPS
    /// </summary>
    public string? CertificatePath { get; set; }

    /// <summary>
    /// Password for SSL certificate
    /// </summary>
    public string? CertificatePassword { get; set; }

    /// <summary>
    /// Maximum number of concurrent connections
    /// </summary>
    public int MaxConcurrentConnections { get; set; } = 100;

    /// <summary>
    /// Maximum number of concurrent upgraded connections (WebSocket, etc.)
    /// </summary>
    public int MaxConcurrentUpgradedConnections { get; set; } = 100;

    /// <summary>
    /// Maximum request body size in bytes (30MB default)
    /// </summary>
    public long MaxRequestBodySize { get; set; } = 30 * 1024 * 1024;

    /// <summary>
    /// Request headers timeout in seconds
    /// </summary>
    public int RequestHeadersTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Keep-alive timeout in seconds
    /// </summary>
    public int KeepAliveTimeoutSeconds { get; set; } = 120;
}