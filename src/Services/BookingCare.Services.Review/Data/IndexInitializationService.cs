using MongoDB.Driver;
using BookingCare.Services.Review.Models.Entities;

namespace BookingCare.Services.Review.Data;

/// <summary>
/// Service for initializing MongoDB indexes
/// </summary>
public interface IIndexInitializationService
{
    /// <summary>
    /// Initializes all required indexes for the Review service
    /// </summary>
    Task InitializeIndexesAsync();
}

/// <summary>
/// Service for initializing MongoDB indexes implementation
/// </summary>
public class IndexInitializationService : IIndexInitializationService
{
    private readonly IMongoCollection<ReviewEntity> _reviewsCollection;

    public IndexInitializationService(IReviewDbContext dbContext)
    {
        _reviewsCollection = dbContext.GetCollection<ReviewEntity>("reviews");
    }

    /// <summary>
    /// Initializes all required indexes for the Review service
    /// Only creates indexes for PatientId, DoctorId, and ClinicServiceId as specified
    /// </summary>
    public async Task InitializeIndexesAsync()
    {
        // Create indexes only for the three specified fields
        var indexes = new List<CreateIndexModel<ReviewEntity>>
        {
            // Individual indexes for PatientId, DoctorId, and ClinicServiceId
            new CreateIndexModel<ReviewEntity>(
                Builders<ReviewEntity>.IndexKeys.Ascending(r => r.PatientId),
                new CreateIndexOptions { Name = "idx_patientId", Background = true }
            ),

            new CreateIndexModel<ReviewEntity>(
                Builders<ReviewEntity>.IndexKeys.Ascending(r => r.DoctorId),
                new CreateIndexOptions { Name = "idx_doctorId", Background = true, Sparse = true }
            ),

            new CreateIndexModel<ReviewEntity>(
                Builders<ReviewEntity>.IndexKeys.Ascending(r => r.ClinicServiceId),
                new CreateIndexOptions { Name = "idx_clinicServiceId", Background = true, Sparse = true }
            )
        };

        try
        {
            await _reviewsCollection.Indexes.CreateManyAsync(indexes);
        }
        catch (MongoCommandException ex) when (ex.Code == 85) // IndexOptionsConflict
        {
            // Index already exists, which is fine
        }
    }
}