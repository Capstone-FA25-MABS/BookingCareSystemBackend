using MongoDB.Driver;

namespace BookingCare.Services.Review.Data;

/// <summary>
/// MongoDB database context for Review service
/// </summary>
public interface IReviewDbContext
{
    /// <summary>
    /// Gets the MongoDB database instance
    /// </summary>
    IMongoDatabase Database { get; }

    /// <summary>
    /// Gets a collection of the specified type
    /// </summary>
    /// <typeparam name="T">The type of documents in the collection</typeparam>
    /// <param name="name">The name of the collection</param>
    /// <returns>The collection</returns>
    IMongoCollection<T> GetCollection<T>(string name);
}

/// <summary>
/// MongoDB database context implementation for Review service
/// </summary>
public class ReviewDbContext : IReviewDbContext
{
    public IMongoDatabase Database { get; }

    public ReviewDbContext(IMongoDatabase database)
    {
        Database = database;
    }

    public IMongoCollection<T> GetCollection<T>(string name)
    {
        return Database.GetCollection<T>(name);
    }
}