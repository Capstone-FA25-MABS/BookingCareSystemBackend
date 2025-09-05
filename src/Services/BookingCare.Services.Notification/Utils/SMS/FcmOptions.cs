namespace BookingCare.Services.Notification.Utils.SMS;

public class FcmOptions
{
    public const string SectionName = "Fcm";
    public string ServiceAccountPath { get; set; } = "";
    public string ProjectId { get; set; } = "";
}
