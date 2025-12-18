using System.Collections.ObjectModel;
using BookingCare.Shared.Common.AppRouting;

namespace BookingCare.Shared.Common.Configuration;

/// <summary>
/// Centralized Frontend configuration for all services
/// </summary>
public static class FrontendConfiguration
{
    /// <summary>
    /// Default Frontend configuration values (for local development)
    /// </summary>
    public static class Defaults
    {
        public const string ClientBaseUrl = "https://medcure.com.vn/";
        public const string AdminBaseUrl = "https://admin.medcure.com.vn/";
        public const string DefaultBaseUrl = "https://medcure.com.vn/";

        public static readonly IReadOnlyDictionary<string, string> HostMap =
            new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
            {
                { "admin.medcure.com.vn", "admin" },
                { "medcure.com.vn", "client" }
            });
    }

    /// <summary>
    /// Create FrontendOptions object using default values
    /// </summary>
    /// <returns>FrontendOptions object</returns>
    public static FrontendOptions CreateFrontendOptions()
    {
        return new FrontendOptions
        {
            Client = new FrontendTarget { BaseUrl = Defaults.ClientBaseUrl },
            Admin = new FrontendTarget { BaseUrl = Defaults.AdminBaseUrl },
            Default = new FrontendTarget { BaseUrl = Defaults.DefaultBaseUrl },
            HostMap = new Dictionary<string, string>(Defaults.HostMap)
        };
    }
}
