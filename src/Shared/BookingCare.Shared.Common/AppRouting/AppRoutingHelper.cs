namespace BookingCare.Shared.Common.AppRouting
{
    public class FrontendOptions
    {
        public const string SectionName = "FrontendOptions";
        public FrontendTarget Client { get; set; } = new();
        public FrontendTarget Admin { get; set; } = new();
        public FrontendTarget Default { get; set; } = new();
        public Dictionary<string, string> HostMap { get; set; } = new(); // host -> prefix
    }

    public class FrontendTarget
    {
        public string BaseUrl { get; set; } = string.Empty;
    }

    public static class AppRoutingHelper
    {
        public static string GetAppPrefix(string origin, FrontendOptions options)
        {
            if (!string.IsNullOrEmpty(origin))
            {
                try
                {
                    var uri = new Uri(origin);
                    var host = uri.Host.ToLowerInvariant();
                    var hostWithPort = $"{host}:{uri.Port}";

                    // Try matching with port first (e.g., "localhost:5173")
                    if (options.HostMap.TryGetValue(hostWithPort, out var mapped))
                    {
                        return mapped;
                    }

                    // Fallback: try matching host only (e.g., "medcure.com.vn")
                    // This handles production domains where port is default (80/443)
                    if (options.HostMap.TryGetValue(host, out mapped))
                    {
                        return mapped;
                    }
                }
                catch
                {
                    // Ignore invalid URI format
                }
            }
            return "default";
        }

        public static string ResolveBaseUrl(string prefix, FrontendOptions options)
        {
            return prefix switch
            {
                "admin" => options.Admin.BaseUrl,
                "client" => options.Client.BaseUrl,
                _ => options.Default.BaseUrl
            };
        }
    }
}
