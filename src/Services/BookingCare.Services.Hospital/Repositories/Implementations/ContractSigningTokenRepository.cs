using BookingCare.Services.Hospital.Data;
using BookingCare.Services.Hospital.Models.Entities;
using BookingCare.Services.Hospital.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.Hospital.Repositories.Implementations;

/// <summary>
/// Repository implementation for contract signing token operations
/// </summary>
public class ContractSigningTokenRepository : IContractSigningTokenRepository
{
    private readonly HospitalDbContext _context;
    private readonly ILogger<ContractSigningTokenRepository> _logger;

    public ContractSigningTokenRepository(
        HospitalDbContext context,
        ILogger<ContractSigningTokenRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ContractSigningTokenEntity?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _context.ContractSigningTokens
                .Include(t => t.Registration)
                .FirstOrDefaultAsync(t => t.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contract signing token by ID: {Id}", id);
            throw;
        }
    }

    public async Task<ContractSigningTokenEntity?> GetByTokenAsync(string token)
    {
        try
        {
            return await _context.ContractSigningTokens
                .Include(t => t.Registration)
                .FirstOrDefaultAsync(t => t.Token == token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contract signing token by token string");
            throw;
        }
    }

    public async Task<ContractSigningTokenEntity?> GetByRegistrationIdAsync(Guid registrationId)
    {
        try
        {
            return await _context.ContractSigningTokens
                .Where(t => t.RegistrationId == registrationId)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contract signing token by registration ID: {RegistrationId}", registrationId);
            throw;
        }
    }

    public async Task<ContractSigningTokenEntity?> GetActiveTokenByRegistrationIdAsync(Guid registrationId)
    {
        try
        {
            var now = DateTime.UtcNow;
            return await _context.ContractSigningTokens
                .Where(t => t.RegistrationId == registrationId
                    && !t.IsUsed
                    && t.ExpiresAt > now)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active token by registration ID: {RegistrationId}", registrationId);
            throw;
        }
    }

    public async Task<ContractSigningTokenEntity> CreateAsync(ContractSigningTokenEntity token)
    {
        try
        {
            await _context.ContractSigningTokens.AddAsync(token);
            await _context.SaveChangesAsync();
            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating contract signing token for registration: {RegistrationId}", token.RegistrationId);
            throw;
        }
    }

    public async Task<ContractSigningTokenEntity> MarkAsUsedAsync(Guid tokenId, string ipAddress, string userAgent)
    {
        try
        {
            var token = await GetByIdAsync(tokenId);
            if (token == null)
            {
                throw new InvalidOperationException($"Token with ID {tokenId} not found");
            }

            token.IsUsed = true;
            token.UsedAt = DateTime.UtcNow;
            token.SignedFromIp = ipAddress;
            token.UserAgent = userAgent;

            _context.ContractSigningTokens.Update(token);
            await _context.SaveChangesAsync();

            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking token as used: {TokenId}", tokenId);
            throw;
        }
    }

    public async Task<int> DeleteExpiredTokensAsync()
    {
        try
        {
            var now = DateTime.UtcNow;
            var expiredTokens = await _context.ContractSigningTokens
                .Where(t => t.ExpiresAt < now && !t.IsUsed)
                .ToListAsync();

            if (expiredTokens.Any())
            {
                _context.ContractSigningTokens.RemoveRange(expiredTokens);
                await _context.SaveChangesAsync();
            }

            return expiredTokens.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting expired tokens");
            throw;
        }
    }

    public async Task InvalidateTokensByRegistrationIdAsync(Guid registrationId)
    {
        try
        {
            var tokens = await _context.ContractSigningTokens
                .Where(t => t.RegistrationId == registrationId && !t.IsUsed)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.IsUsed = true;
                token.UsedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating tokens for registration: {RegistrationId}", registrationId);
            throw;
        }
    }
}
