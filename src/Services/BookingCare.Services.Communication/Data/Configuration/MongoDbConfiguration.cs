using MongoDB.Driver;

namespace BookingCare.Services.Communication.Data.Configuration;

/// <summary>
/// Configuration cho MongoDB connection
/// </summary>
public class MongoDbConfiguration
{
    /// <summary>
    /// Connection string cho MongoDB
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Tên database
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;
}

/// <summary>
/// MongoDB connection manager
/// </summary>
public class MongoDbConnection
{
    private readonly MongoDbConfiguration _configuration;
    private readonly IMongoClient _client;
    private readonly IMongoDatabase _database;

    public MongoDbConnection(MongoDbConfiguration configuration)
    {
        _configuration = configuration;
        _client = new MongoClient(_configuration.ConnectionString);
        _database = _client.GetDatabase(_configuration.DatabaseName);
    }

    /// <summary>
    /// L?y MongoDB database instance
    /// </summary>
    public IMongoDatabase Database => _database;

    /// <summary>
    /// L?y MongoDB client instance
    /// </summary>
    public IMongoClient Client => _client;
}