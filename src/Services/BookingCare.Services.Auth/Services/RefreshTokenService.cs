using BookingCare.Services.Auth.Models.Entities;
using BookingCare.Services.Auth.Repositories;
using BookingCare.Services.Auth.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BookingCare.Services.Auth.Services;

/// <summary>
/// Service for managing refresh tokens with high security
/// </summary>
public class RefreshTokenService
{
    private readonly AuthDbContext _context;
    private readonly JwtService _jwtService;
    private readonly ILogger<RefreshTokenService> _logger;

    public RefreshTokenService(
        AuthDbContext context,
        JwtService jwtService,
        ILogger<RefreshTokenService> logger)
    {
        _context = context;
        _jwtService = jwtService;
        _logger = logger;
    }

    /// <summary>
    /// Create new refresh token for account
    /// </summary>
    public async Task<RefreshTokenEntity> CreateRefreshTokenAsync(Guid accountId)
    {
        try
        {
            // Ensure only one active refresh token per account
            var existingTokens = await _context.RefreshTokens
                .Where(rt => rt.AccountId == accountId)
                .ToListAsync();
            if (existingTokens.Count > 0)
            {
                _context.RefreshTokens.RemoveRange(existingTokens);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted {Count} existing refresh tokens for account: {AccountId}", existingTokens.Count, accountId);
            }

            // Generate a unique refresh token with retries to avoid rare collisions
            const int maxAttempts = 5;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var candidateToken = _jwtService.GenerateRefreshToken();

                // Fast existence check to reduce likelihood of hitting DB unique constraint
                var exists = await _context.RefreshTokens.AnyAsync(rt => rt.Token == candidateToken);
                if (exists)
                {
                    _logger.LogWarning("Collision detected for refresh token on attempt {Attempt}. Retrying...", attempt);
                    continue;
                }

                var refreshToken = new RefreshTokenEntity
                {
                    Id = Guid.NewGuid(),
                    Token = candidateToken,
                    AccountId = accountId,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(_jwtService.GetRefreshTokenExpirationDays())
                };

                _context.RefreshTokens.Add(refreshToken);
                try
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Created refresh token for account: {AccountId}", accountId);
                    return refreshToken;
                }
                catch (DbUpdateException dbEx)
                {
                    // Handle race-condition collision on unique index
                    if (dbEx.InnerException?.Message.Contains("IX_RefreshTokens_Token") == true)
                    {
                        _logger.LogWarning(dbEx, "Unique index collision for refresh token on attempt {Attempt}. Retrying...", attempt);
                        _context.Entry(refreshToken).State = EntityState.Detached;
                        continue;
                    }
                    throw;
                }
            }

            // If we got here, something is abnormal
            throw new InvalidOperationException("Failed to generate a unique refresh token after multiple attempts");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating refresh token for account: {AccountId}", accountId);
            throw;
        }
    }

    /// <summary>
    /// Validate refresh token and return account
    /// </summary>
    public async Task<(bool IsValid, AccountEntity? Account, RefreshTokenEntity? Token)> ValidateRefreshTokenAsync(string token)
    {
        try
        {
            var refreshToken = await _context.RefreshTokens
                .Include(rt => rt.Account)
                .FirstOrDefaultAsync(rt => rt.Token == token);

            if (refreshToken == null)
            {
                _logger.LogWarning("Refresh token not found: {Token}", token[..8] + "...");
                return (false, null, null);
            }

            if (refreshToken.IsExpired)
            {
                _logger.LogWarning("Refresh token is expired: {TokenId}", refreshToken.Id);
                await DeleteRefreshTokenAsync(token);
                return (false, null, refreshToken);
            }
    
            _logger.LogInformation("Refresh token validated successfully: {TokenId}", refreshToken.Id);
            return (true, refreshToken.Account, refreshToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating refresh token");
            return (false, null, null);
        }
    }

    /// <summary>
    /// Delete a refresh token by raw token string
    /// </summary>
    public async Task<bool> DeleteRefreshTokenAsync(string token)
    {
        try
        {
            var entity = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token);
            if (entity == null)
            {
                _logger.LogWarning("Delete skipped. Refresh token not found: {TokenPrefix}", token.Length > 8 ? token[..8] + "..." : token);
                return false;
            }
            _context.RefreshTokens.Remove(entity);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Deleted refresh token by string: {TokenId}", entity.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting refresh token by string");
            return false;
        }
    }
    
    /// <summary>
    /// Clean up expired refresh tokens
    /// </summary>
    public async Task<int> CleanupExpiredTokensAsync()
    {
        try
        {
            var expiredTokens = await _context.RefreshTokens
                .Where(rt => rt.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();

            _context.RefreshTokens.RemoveRange(expiredTokens);
            var deletedCount = await _context.SaveChangesAsync();

            _logger.LogInformation("Cleaned up {Count} expired refresh tokens", deletedCount);
            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired refresh tokens");
            return 0;
        }
    }
    
}
