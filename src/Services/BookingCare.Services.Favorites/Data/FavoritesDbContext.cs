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
        // Create compound index for patient_id and doctor_id to prevent duplicates
        var indexKeysDefinition = Builders<Models.Entities.FavoriteEntity>.IndexKeys
            .Ascending(f => f.PatientId)
            .Ascending(f => f.DoctorId);

        var indexOptions = new CreateIndexOptions
        {
            Unique = true,
            Name = "patient_doctor_unique_idx"
        };

        await Favorites.Indexes.CreateOneAsync(
            new CreateIndexModel<Models.Entities.FavoriteEntity>(indexKeysDefinition, indexOptions));

        // Create index on patient_id for fast queries
        var patientIndexKeys = Builders<Models.Entities.FavoriteEntity>.IndexKeys
            .Ascending(f => f.PatientId);

        var patientIndexOptions = new CreateIndexOptions
        {
            Name = "patient_id_idx"
        };

        await Favorites.Indexes.CreateOneAsync(
            new CreateIndexModel<Models.Entities.FavoriteEntity>(patientIndexKeys, patientIndexOptions));

        // Create index on doctor_id for analytics
        var doctorIndexKeys = Builders<Models.Entities.FavoriteEntity>.IndexKeys
            .Ascending(f => f.DoctorId);

        var doctorIndexOptions = new CreateIndexOptions
        {
            Name = "doctor_id_idx"
        };

        await Favorites.Indexes.CreateOneAsync(
            new CreateIndexModel<Models.Entities.FavoriteEntity>(doctorIndexKeys, doctorIndexOptions));

        // Create index on created_at for time-based queries
        var createdAtIndexKeys = Builders<Models.Entities.FavoriteEntity>.IndexKeys
            .Descending(f => f.CreatedAt);

        var createdAtIndexOptions = new CreateIndexOptions
        {
            Name = "created_at_idx"
        };

        await Favorites.Indexes.CreateOneAsync(
            new CreateIndexModel<Models.Entities.FavoriteEntity>(createdAtIndexKeys, createdAtIndexOptions));
    }
}