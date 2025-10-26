namespace BookingCare.Services.Auth.Configuration;

/// <summary>
/// Configuration for default avatar URLs
/// </summary>
public class DefaultAvatarsOptions
{
    public const string SectionName = "DefaultAvatars";

    public AvatarUrls User { get; set; } = new();
}

/// <summary>
/// Avatar URLs for male and female
/// </summary>
public class AvatarUrls
{
    public string Male { get; set; } = string.Empty;
    public string Female { get; set; } = string.Empty;
}

