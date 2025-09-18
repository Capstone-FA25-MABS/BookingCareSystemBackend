using System.Collections.ObjectModel;
using BookingCare.Shared.Common.AppRouting;
namespace BookingCare.Shared.Common.Configuration;

/// <summary>
/// Centralized Frontend configuration for all services
/// </summary>
public static class FrontendConfiguration
{
    /// <summary>
    /// Default Frontend configuration values
    /// </summary>
    public static class Defaults
    {
        public const string ClientBaseUrl = "http://localhost:3000/";
        public const string AdminBaseUrl = "http://localhost:5174/";
        public const string DefaultBaseUrl = "http://localhost:3000/";

        public static readonly IReadOnlyDictionary<string, string> HostMap =
            new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
            {
                { "localhost:5174", "admin" },
                { "localhost:3000", "client" }
            });
    }

    /// <summary>
    /// Get Frontend configuration from environment variables or use defaults
    /// </summary>
    /// <returns>Frontend configuration values</returns>
    public static (string ClientBaseUrl, string AdminBaseUrl, string DefaultBaseUrl, IReadOnlyDictionary<string, string> HostMap) GetConfiguration()
    {
        return (
            ClientBaseUrl: Environment.GetEnvironmentVariable("FRONTEND_CLIENT_BASE_URL") ?? Defaults.ClientBaseUrl,
            AdminBaseUrl: Environment.GetEnvironmentVariable("FRONTEND_ADMIN_BASE_URL") ?? Defaults.AdminBaseUrl,
            DefaultBaseUrl: Environment.GetEnvironmentVariable("FRONTEND_DEFAULT_BASE_URL") ?? Defaults.DefaultBaseUrl,
            HostMap: ParseHostMap(Environment.GetEnvironmentVariable("FRONTEND_HOST_MAP")) ?? Defaults.HostMap
        );
    }

    /// <summary>
    /// Get Frontend configuration from IConfiguration with fallback to defaults
    /// </summary>
    /// <param name="configuration">Configuration instance</param>
    /// <returns>Frontend configuration values</returns>
    public static (string ClientBaseUrl, string AdminBaseUrl, string DefaultBaseUrl, IReadOnlyDictionary<string, string> HostMap) GetConfiguration(Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        return (
            ClientBaseUrl: configuration["FrontendOptions:Client:BaseUrl"] ?? Environment.GetEnvironmentVariable("FRONTEND_CLIENT_BASE_URL") ?? Defaults.ClientBaseUrl,
            AdminBaseUrl: configuration["FrontendOptions:Admin:BaseUrl"] ?? Environment.GetEnvironmentVariable("FRONTEND_ADMIN_BASE_URL") ?? Defaults.AdminBaseUrl,
            DefaultBaseUrl: configuration["FrontendOptions:Default:BaseUrl"] ?? Environment.GetEnvironmentVariable("FRONTEND_DEFAULT_BASE_URL") ?? Defaults.DefaultBaseUrl,
            HostMap: ParseHostMap(configuration["FrontendOptions:HostMap"] ?? Environment.GetEnvironmentVariable("FRONTEND_HOST_MAP")) ?? Defaults.HostMap
        );
    }

    /// <summary>
    /// Create FrontendOptions object from configuration
    /// </summary>
    /// <param name="configuration">Configuration instance (optional)</param>
    /// <returns>FrontendOptions object</returns>
    public static FrontendOptions CreateFrontendOptions(Microsoft.Extensions.Configuration.IConfiguration? configuration = null)
    {
        var (clientBaseUrl, adminBaseUrl, defaultBaseUrl, hostMap) = configuration != null
            ? GetConfiguration(configuration)
            : GetConfiguration();

        return new FrontendOptions
        {
            Client = new FrontendTarget { BaseUrl = clientBaseUrl },
            Admin = new FrontendTarget { BaseUrl = adminBaseUrl },
            Default = new FrontendTarget { BaseUrl = defaultBaseUrl },
            HostMap = new Dictionary<string, string>(hostMap)
        };
    }

    /// <summary>
    /// Parse host map from environment variable or configuration string
    /// Format: "host1:prefix1,host2:prefix2"
    /// </summary>
    /// <param name="hostMapString">Host map string</param>
    /// <returns>Dictionary of host mappings or null if invalid</returns>
    private static Dictionary<string, string>? ParseHostMap(string? hostMapString)
    {
        if (string.IsNullOrEmpty(hostMapString))
            return null;

        try
        {
            var hostMap = new Dictionary<string, string>();
            var pairs = hostMapString.Split(',', StringSplitOptions.RemoveEmptyEntries);

            foreach (var pair in pairs)
            {
                var parts = pair.Split(':', 2);
                if (parts.Length == 2)
                {
                    hostMap[parts[0].Trim()] = parts[1].Trim();
                }
            }

            return hostMap.Count > 0 ? hostMap : null;
        }
        catch
        {
            return null;
        }
    }
}
