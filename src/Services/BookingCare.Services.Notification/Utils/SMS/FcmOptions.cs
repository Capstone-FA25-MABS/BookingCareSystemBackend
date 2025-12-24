namespace BookingCare.Services.Notification.Utils.SMS;

public class FcmOptions
{
    public const string SectionName = "Fcm";

    /// <summary>
    /// Path to service account JSON file (for local development).
    /// </summary>
    public string ServiceAccountPath { get; set; } = "";

    /// <summary>
    /// Firebase project ID.
    /// </summary>
    public string ProjectId { get; set; } = "";

    /// <summary>
    /// Service account JSON content as string (for production/AWS deployment).
    /// This takes priority over ServiceAccountPath if provided.
    /// Can be set via environment variable: Fcm__ServiceAccountJson
    /// </summary>
    public string ServiceAccountJson { get; set; } = "";
}