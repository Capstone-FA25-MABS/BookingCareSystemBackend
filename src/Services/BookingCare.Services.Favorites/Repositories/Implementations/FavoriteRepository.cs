using BookingCare.Services.Favorites.Data;
using BookingCare.Services.Favorites.Models.Entities;
using BookingCare.Services.Favorites.Repositories.Interfaces;
using BookingCare.Shared.Common.Models;
using MongoDB.Driver;

namespace BookingCare.Services.Favorites.Repositories.Implementations;

/// <summary>
/// Repository implementation for Favorite operations using MongoDB
/// </summary>
public class FavoriteRepository : IFavoriteRepository
{
    private readonly FavoritesDbContext _context;
    private readonly ILogger<FavoriteRepository> _logger;

    public FavoriteRepository(FavoritesDbContext context, ILogger<FavoriteRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<FavoriteEntity> CreateAsync(FavoriteEntity favorite)
    {
        try
        {
            await _context.Favorites.InsertOneAsync(favorite);
            _logger.LogInformation("Created favorite with ID {FavoriteId} for Patient {PatientId} and Doctor {DoctorId}",
                favorite.Id, favorite.PatientId, favorite.DoctorId);
            return favorite;
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            _logger.LogWarning("Attempted to create duplicate favorite for Patient {PatientId} and Doctor {DoctorId}",
                favorite.PatientId, favorite.DoctorId);
            throw new InvalidOperationException("Favorite already exists for this patient and doctor combination.");
        }
    }

    /// <inheritdoc />
    public async Task<FavoriteEntity?> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Retrieving favorite by ID {FavoriteId}", id);
        var filter = Builders<FavoriteEntity>.Filter.Eq(f => f.Id, id);
        return await _context.Favorites.Find(filter).FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<FavoriteEntity?> GetByPatientAndDoctorAsync(Guid patientId, Guid doctorId)
    {
        var filter = Builders<FavoriteEntity>.Filter.And(
            Builders<FavoriteEntity>.Filter.Eq(f => f.PatientId, patientId),
            Builders<FavoriteEntity>.Filter.Eq(f => f.DoctorId, doctorId)
        );

        return await _context.Favorites.Find(filter).FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<List<Guid>> CheckMultipleFavoritesAsync(Guid patientId, List<Guid> doctorIds)
    {
        if (doctorIds == null || !doctorIds.Any())
        {
            _logger.LogWarning("Empty doctor IDs list provided for patient {PatientId}", patientId);
            return new List<Guid>();
        }

        var filter = Builders<FavoriteEntity>.Filter.And(
            Builders<FavoriteEntity>.Filter.Eq(f => f.PatientId, patientId),
            Builders<FavoriteEntity>.Filter.In(f => f.DoctorId, doctorIds)
        );

        try
        {
            // Option 1: Use projection with proper expression (most efficient)
            var projection = Builders<FavoriteEntity>.Projection
                .Expression(f => f.DoctorId);

            var favoritedDoctorIds = await _context.Favorites
                .Find(filter)
                .Project(projection)
                .ToListAsync();

            _logger.LogInformation("Checked {TotalDoctors} doctors for Patient {PatientId}, found {FavoritedCount} favorites",
                doctorIds.Count, patientId, favoritedDoctorIds.Count);

            return favoritedDoctorIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking multiple favorites for Patient {PatientId}", patientId);
            
            // Fallback to full entity approach if projection fails
            _logger.LogInformation("Falling back to full entity retrieval for Patient {PatientId}", patientId);
            
            var favorites = await _context.Favorites
                .Find(filter)
                .ToListAsync();

            return favorites.Select(f => f.DoctorId).ToList();
        }
    }

    /// <inheritdoc />
    public async Task<PagedResult<FavoriteEntity>> GetPatientFavoritesAsync(Guid patientId, int page = 1, int pageSize = 20)
    {
        var filter = Builders<FavoriteEntity>.Filter.Eq(f => f.PatientId, patientId);
        var sort = Builders<FavoriteEntity>.Sort.Descending(f => f.CreatedAt);

        var totalCount = await _context.Favorites.CountDocumentsAsync(filter);
        var items = await _context.Favorites
            .Find(filter)
            .Sort(sort)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return new PagedResult<FavoriteEntity>
        {
            Items = items,
            TotalCount = (int)totalCount,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    /// <inheritdoc />
    public async Task<PagedResult<FavoriteEntity>> GetDoctorFavoritesAsync(Guid doctorId, int page = 1, int pageSize = 20)
    {
        var filter = Builders<FavoriteEntity>.Filter.Eq(f => f.DoctorId, doctorId);
        var sort = Builders<FavoriteEntity>.Sort.Descending(f => f.CreatedAt);

        var totalCount = await _context.Favorites.CountDocumentsAsync(filter);
        var items = await _context.Favorites
            .Find(filter)
            .Sort(sort)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return new PagedResult<FavoriteEntity>
        {
            Items = items,
            TotalCount = (int)totalCount,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    /// <inheritdoc />
    public async Task<bool> RemoveAsync(Guid patientId, Guid doctorId)
    {
        var filter = Builders<FavoriteEntity>.Filter.And(
            Builders<FavoriteEntity>.Filter.Eq(f => f.PatientId, patientId),
            Builders<FavoriteEntity>.Filter.Eq(f => f.DoctorId, doctorId)
        );

        var result = await _context.Favorites.DeleteOneAsync(filter);

        if (result.DeletedCount > 0)
        {
            _logger.LogInformation("Removed favorite for Patient {PatientId} and Doctor {DoctorId}",
                patientId, doctorId);
            return true;
        }

        _logger.LogWarning("Attempted to remove non-existent favorite for Patient {PatientId} and Doctor {DoctorId}",
            patientId, doctorId);
        return false;
    }

    /// <inheritdoc />
    public async Task<bool> IsFavoritedAsync(Guid patientId, Guid doctorId)
    {
        var filter = Builders<FavoriteEntity>.Filter.And(
            Builders<FavoriteEntity>.Filter.Eq(f => f.PatientId, patientId),
            Builders<FavoriteEntity>.Filter.Eq(f => f.DoctorId, doctorId)
        );

        return await _context.Favorites.Find(filter).AnyAsync();
    }

    /// <inheritdoc />
    public async Task<long> GetDoctorFavoriteCountAsync(Guid doctorId)
    {
        var filter = Builders<FavoriteEntity>.Filter.Eq(f => f.DoctorId, doctorId);
        return await _context.Favorites.CountDocumentsAsync(filter);
    }

    /// <inheritdoc />
    public async Task<List<FavoriteEntity>> GetRecentFavoritesAsync(Guid patientId, int limit = 5)
    {
        var filter = Builders<FavoriteEntity>.Filter.Eq(f => f.PatientId, patientId);
        var sort = Builders<FavoriteEntity>.Sort.Descending(f => f.CreatedAt);

        return await _context.Favorites
            .Find(filter)
            .Sort(sort)
            .Limit(limit)
            .ToListAsync();
    }
}