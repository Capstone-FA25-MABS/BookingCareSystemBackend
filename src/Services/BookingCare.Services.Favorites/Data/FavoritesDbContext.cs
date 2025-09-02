using MongoDB.Driver;

namespace BookingCare.Services.Favorites.Data;

/// <summary>
/// MongoDB context for Favorites database
/// </summary>
public class FavoritesDbContext
{
    private readonly IMongoDatabase _database;

    public FavoritesDbContext(IMongoDatabase database)
    {
        _database = database;
    }

    /// <summary>
    /// Favorites collection
    /// </summary>
    public IMongoCollection<Models.Entities.FavoriteEntity> Favorites =>
        _database.GetCollection<Models.Entities.FavoriteEntity>("favorites");

    /// <summary>
    /// Create indexes for the database
    /// </summary>
    public async Task CreateIndexesAsync()
    {
        // Create index on patient_id for fast queries
        var patientIndexKeys = Builders<Models.Entities.FavoriteEntity>.IndexKeys
            .Ascending(f => f.PatientId);

        var patientIndexOptions = new CreateIndexOptions
        {
            Name = "patient_id_idx"
        };

        await Favorites.Indexes.CreateOneAsync(
            new CreateIndexModel<Models.Entities.FavoriteEntity>(patientIndexKeys, patientIndexOptions));
    }
}