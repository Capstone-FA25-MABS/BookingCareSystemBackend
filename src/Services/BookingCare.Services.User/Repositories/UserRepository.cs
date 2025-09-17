using BookingCare.Services.User.Data;
using BookingCare.Services.User.Exceptions;
using BookingCare.Services.User.Models.DTOs;
using BookingCare.Services.User.Models.Entities;
using Microsoft.EntityFrameworkCore;


namespace BookingCare.Services.User.Repositories;

public class UserRepository : IUserRepository
{
    private readonly UserDbContext _context;
    private readonly ILogger<UserRepository> _logger;
    private const string ServiceName = "UserService";

    public UserRepository(UserDbContext context, ILogger<UserRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UserEntity?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ServiceName}] Database error when getting user by ID: {UserId}", ServiceName, id);
            throw new UserException($"[{ServiceName}] Failed to retrieve user by ID: {id}", innerException: ex);
        }
    }

    public async Task<UserEntity?> GetByAccountIdAsync(Guid accountId)
    {
        try
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.AccountId == accountId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ServiceName}] Database error when getting user by AccountId: {AccountId}", ServiceName, accountId);
            throw new UserException($"[{ServiceName}] Failed to retrieve user by AccountId: {accountId}", innerException: ex);
        }
    }

    public async Task<UserEntity> CreateAsync(UserEntity user)
    {
        try
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ServiceName}] Database error when creating user with email: {Email}", ServiceName, user.Email);
            throw new UserException($"[{ServiceName}] Failed to create user with email: {user.Email}", innerException: ex);
        }
    }

    public async Task<UserEntity> UpdateAsync(UserEntity user)
    {
        try
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "[{ServiceName}] Concurrency conflict when updating user: {UserId}", ServiceName, user.Id);
            throw new UserException($"[{ServiceName}] User was modified by another process. Please refresh and try again. UserId: {user.Id}", innerException: ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ServiceName}] Database error when updating user: {UserId}", ServiceName, user.Id);
            throw new UserException($"[{ServiceName}] Failed to update user: {user.Id}", innerException: ex);
        }
    }

    public async Task<(List<UserEntity> Users, int TotalCount)> GetUsersAsync(UserQueryRequest query)
    {
        try
        {
            var queryable = _context.Users.AsQueryable();

            // Apply filters
            if (query.Gender.HasValue)
            {
                queryable = queryable.Where(u => u.Gender == query.Gender.Value);
            }

            if (!string.IsNullOrEmpty(query.SearchTerm))
            {
                queryable = queryable.Where(u =>
                    u.FirstName.Contains(query.SearchTerm) ||
                    u.LastName.Contains(query.SearchTerm) ||
                    u.Email.Contains(query.SearchTerm) ||
                    (u.Address != null && u.Address.Contains(query.SearchTerm)) ||
                    u.Phone.Contains(query.SearchTerm));
            }

            if (query.CreatedFrom.HasValue)
            {
                queryable = queryable.Where(u => u.CreatedAt >= query.CreatedFrom.Value);
            }

            if (query.CreatedTo.HasValue)
            {
                queryable = queryable.Where(u => u.CreatedAt <= query.CreatedTo.Value);
            }

            var totalCount = await queryable.CountAsync();

            // Apply sorting
            queryable = query.SortBy?.ToLower() switch
            {
                "firstname" => query.SortDescending ? queryable.OrderByDescending(u => u.FirstName) : queryable.OrderBy(u => u.FirstName),
                "lastname" => query.SortDescending ? queryable.OrderByDescending(u => u.LastName) : queryable.OrderBy(u => u.LastName),
                "email" => query.SortDescending ? queryable.OrderByDescending(u => u.Email) : queryable.OrderBy(u => u.Email),
                "createdat" => query.SortDescending ? queryable.OrderByDescending(u => u.CreatedAt) : queryable.OrderBy(u => u.CreatedAt),
                "updatedat" => query.SortDescending ? queryable.OrderByDescending(u => u.UpdatedAt) : queryable.OrderBy(u => u.UpdatedAt),
                _ => queryable.OrderByDescending(u => u.CreatedAt)
            };

            var users = await queryable
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return (users, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ServiceName}] Database error when querying users with filter: {SearchTerm}", ServiceName, query.SearchTerm);
            throw new UserException($"[{ServiceName}] Failed to query users", innerException: ex);
        }
    }

    public async Task<List<UserEntity>> SearchUsersAsync(string searchTerm, int limit = 10)
    {
        try
        {
            return await _context.Users
                .Where(u =>
                    u.FirstName.Contains(searchTerm) ||
                    u.LastName.Contains(searchTerm) ||
                    u.Email.Contains(searchTerm))
                .Take(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ServiceName}] Database error when searching users with term: {SearchTerm}", ServiceName, searchTerm);
            throw new UserException($"[{ServiceName}] Failed to search users with term: {searchTerm}", innerException: ex);
        }
    }

    public async Task<List<UserBasicInfoResponse>> GetUsersByAccountIdsAsync(List<Guid> accountIds)
    {
        try
        {
            if (accountIds == null || accountIds.Count == 0)
            {
                return new List<UserBasicInfoResponse>();
            }

            _logger.LogInformation("[{ServiceName}] Getting users by account IDs batch - Count: {Count}", ServiceName, accountIds.Count);

            // Performance optimized query - select only necessary fields and map directly to DTO
            var users = await _context.Users
                .Where(u => accountIds.Contains(u.AccountId))
                .Select(u => new UserBasicInfoResponse
                {
                    AccountId = u.AccountId,
                    Email = u.Email,
                    FullName = (u.FirstName + " " + u.LastName).Trim(),
                    AvatarUrl = u.AvatarUrl
                })
                .ToListAsync();

            _logger.LogInformation("[{ServiceName}] Retrieved {Count} users for batch request", ServiceName, users.Count);
            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ServiceName}] Database error when getting users by account IDs batch", ServiceName);
            throw new UserException($"[{ServiceName}] Failed to get users by account IDs batch", innerException: ex);
        }
    }

}
