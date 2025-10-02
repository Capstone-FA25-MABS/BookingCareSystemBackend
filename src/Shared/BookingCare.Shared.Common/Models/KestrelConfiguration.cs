
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