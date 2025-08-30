namespace BookingCare.Services.Favorites.Models.Configuration;

/// <summary>
/// MongoDB configuration settings
/// </summary>
public class MongoDbSettings
{
    /// <summary>
    /// MongoDB connection string
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Database name
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;
}