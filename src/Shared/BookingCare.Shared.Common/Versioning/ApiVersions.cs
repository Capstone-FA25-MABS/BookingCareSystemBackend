namespace BookingCare.Shared.Common.Versioning;

/// <summary>
/// API version constants for consistent versioning across services
/// </summary>
public static class ApiVersions
{
    /// <summary>
    /// Version 1.0 - Initial version
    /// </summary>
    public const string V1_0 = "1.0";

    /// <summary>
    /// Version 1.1 - Minor updates and improvements
    /// </summary>
    public const string V1_1 = "1.1";

    /// <summary>
    /// Version 2.0 - Major version with breaking changes
    /// </summary>
    public const string V2_0 = "2.0";

    /// <summary>
    /// Default version
    /// </summary>
    public const string Default = V1_0;

    /// <summary>
    /// Latest stable version
    /// </summary>
    public const string Latest = V1_1;
}

/// <summary>
/// Route templates for versioned APIs
/// </summary>
public static class ApiRouteTemplates
{
    /// <summary>
    /// Versioned route template: api/v{version:apiVersion}/[controller]
    /// </summary>
    public const string Versioned = "api/v{version:apiVersion}/[controller]";

    /// <summary>
    /// Legacy route template: api/[controller] (will default to v1.0)
    /// </summary>
    public const string Legacy = "api/[controller]";
}
