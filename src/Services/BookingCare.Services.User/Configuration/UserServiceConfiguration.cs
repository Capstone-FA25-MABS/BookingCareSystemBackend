namespace BookingCare.Services.User.Configuration;

/// <summary>
/// Configuration for User Service settings
/// </summary>
public class UserServiceConfiguration
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "UserService";

    /// <summary>
    /// Default avatar URL for users
    /// </summary>
    public string DefaultAvatarUrl { get; set; } = "https://d24em9p7s2uixh.cloudfront.net/avatars/patients/male_20251003_f9c91483.png";

    /// <summary>
    /// Default avatar URL for creating new users (fallback)
    /// </summary>
    public string DefaultUserAvatarUrl { get; set; } = "https://bookingcaree.com/user-avatar-default.png";
}
