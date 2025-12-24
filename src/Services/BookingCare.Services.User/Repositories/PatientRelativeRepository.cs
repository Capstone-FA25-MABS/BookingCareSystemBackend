using BookingCare.Services.User.Data;
using BookingCare.Services.User.Exceptions;
using BookingCare.Services.User.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.User.Repositories;

/// <summary>
/// Repository implementation for PatientRelative operations
/// </summary>
public class PatientRelativeRepository : IPatientRelativeRepository
{
    private readonly UserDbContext _context;
    private readonly ILogger<PatientRelativeRepository> _logger;
    private const string ServiceName = "UserService";

    public PatientRelativeRepository(UserDbContext context, ILogger<PatientRelativeRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<PatientRelativeEntity>> GetByUserIdAsync(Guid userId)
    {
        try
        {
            return await _context.PatientRelatives
                .Where(r => r.UserId == userId)
                .OrderBy(r => r.FirstName)
                .ThenBy(r => r.LastName)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ServiceName}] Database error when getting relatives by UserId: {UserId}", ServiceName, userId);
            throw new UserException($"[{ServiceName}] Failed to retrieve relatives by UserId: {userId}", innerException: ex);
        }
    }

    /// <inheritdoc />
    public async Task<PatientRelativeEntity?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _context.PatientRelatives
                .FirstOrDefaultAsync(r => r.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ServiceName}] Database error when getting relative by ID: {RelativeId}", ServiceName, id);
            throw new UserException($"[{ServiceName}] Failed to retrieve relative by ID: {id}", innerException: ex);
        }
    }

    /// <inheritdoc />
    public async Task<PatientRelativeEntity?> GetByIdAndUserIdAsync(Guid id, Guid userId)
    {
        try
        {
            return await _context.PatientRelatives
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ServiceName}] Database error when getting relative by ID and UserId: {RelativeId}, {UserId}", ServiceName, id, userId);
            throw new UserException($"[{ServiceName}] Failed to retrieve relative by ID: {id} and UserId: {userId}", innerException: ex);
        }
    }

    /// <inheritdoc />
    public async Task<PatientRelativeEntity> CreateAsync(PatientRelativeEntity entity)
    {
        try
        {
            _context.PatientRelatives.Add(entity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[{ServiceName}] Created patient relative {RelativeId} for user {UserId}", ServiceName, entity.Id, entity.UserId);
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ServiceName}] Database error when creating relative for user: {UserId}", ServiceName, entity.UserId);
            throw new UserException($"[{ServiceName}] Failed to create relative for user: {entity.UserId}", innerException: ex);
        }
    }

    /// <inheritdoc />
    public async Task<PatientRelativeEntity> UpdateAsync(PatientRelativeEntity entity)
    {
        try
        {
            _context.PatientRelatives.Update(entity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[{ServiceName}] Updated patient relative {RelativeId}", ServiceName, entity.Id);
            return entity;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "[{ServiceName}] Concurrency conflict when updating relative: {RelativeId}", ServiceName, entity.Id);
            throw new UserException($"[{ServiceName}] Relative was modified by another process. Please refresh and try again. RelativeId: {entity.Id}", innerException: ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ServiceName}] Database error when updating relative: {RelativeId}", ServiceName, entity.Id);
            throw new UserException($"[{ServiceName}] Failed to update relative: {entity.Id}", innerException: ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            var entity = await _context.PatientRelatives.FindAsync(id);
            if (entity == null)
            {
                return false;
            }

            _context.PatientRelatives.Remove(entity);
            var result = await _context.SaveChangesAsync();

            if (result > 0)
            {
                _logger.LogInformation("[{ServiceName}] Deleted patient relative {RelativeId}", ServiceName, id);
                return true;
            }
            else
            {
                _logger.LogWarning("[{ServiceName}] No changes made when deleting relative: {RelativeId}", ServiceName, id);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ServiceName}] Database error when deleting relative: {RelativeId}", ServiceName, id);
            throw new UserException($"[{ServiceName}] Failed to delete relative: {id}", innerException: ex);
        }
    }

    /// <inheritdoc />
    public async Task<int> CountByUserIdAsync(Guid userId)
    {
        try
        {
            return await _context.PatientRelatives
                .CountAsync(r => r.UserId == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ServiceName}] Database error when counting relatives for user: {UserId}", ServiceName, userId);
            throw new UserException($"[{ServiceName}] Failed to count relatives for user: {userId}", innerException: ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid id)
    {
        try
        {
            return await _context.PatientRelatives
                .AnyAsync(r => r.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ServiceName}] Database error when checking relative existence: {RelativeId}", ServiceName, id);
            throw new UserException($"[{ServiceName}] Failed to check relative existence: {id}", innerException: ex);
        }
    }

    /// <inheritdoc />
    public async Task<List<PatientRelativeEntity>> GetByIdsAsync(List<Guid> ids)
    {
        try
        {
            if (ids == null || !ids.Any())
            {
                return new List<PatientRelativeEntity>();
            }

            return await _context.PatientRelatives
                .Where(r => ids.Contains(r.Id))
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ServiceName}] Database error when getting relatives by IDs: {Count} IDs", ServiceName, ids?.Count ?? 0);
            throw new UserException($"[{ServiceName}] Failed to retrieve relatives by IDs", innerException: ex);
        }
    }
}
