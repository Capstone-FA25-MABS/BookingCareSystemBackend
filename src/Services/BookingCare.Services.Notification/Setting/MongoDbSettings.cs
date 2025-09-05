namespace BookingCare.Services.Notification.Setting;

public class MongoDbSettings
{
    public const string SectionName = "MongoDb";
    public string ConnectionString { get; set; } = "";
    public string DatabaseName { get; set; } = "";
    public string DevicesCollectionName { get; set; } = "devices";
}
