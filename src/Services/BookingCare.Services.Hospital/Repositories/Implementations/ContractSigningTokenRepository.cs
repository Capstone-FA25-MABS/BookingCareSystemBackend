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

    public ContractSigningTokenRepository(HospitalDbContext context)
    {
        _context = context;
    }

    public async Task<ContractSigningTokenEntity?> GetByIdAsync(Guid id)
    {
        return await _context.ContractSigningTokens
            .Include(t => t.Registration)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<ContractSigningTokenEntity?> GetByTokenAsync(string token)
    {
        return await _context.ContractSigningTokens
            .Include(t => t.Registration)
            .FirstOrDefaultAsync(t => t.Token == token);
    }

    public async Task<ContractSigningTokenEntity?> GetByRegistrationIdAsync(Guid registrationId)
    {
        return await _context.ContractSigningTokens
            .Where(t => t.RegistrationId == registrationId)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<ContractSigningTokenEntity?> GetActiveTokenByRegistrationIdAsync(Guid registrationId)
    {
        var now = DateTime.UtcNow;
        return await _context.ContractSigningTokens
            .Where(t => t.RegistrationId == registrationId
                && !t.IsUsed
                && t.ExpiresAt > now)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<ContractSigningTokenEntity> CreateAsync(ContractSigningTokenEntity token)
    {
        await _context.ContractSigningTokens.AddAsync(token);
        await _context.SaveChangesAsync();
        return token;
    }

    public async Task<ContractSigningTokenEntity> MarkAsUsedAsync(Guid tokenId, string ipAddress, string userAgent)
    {
        var token = await GetByIdAsync(tokenId)
            ?? throw new InvalidOperationException($"Token with ID {tokenId} not found");

        token.IsUsed = true;
        token.UsedAt = DateTime.UtcNow;
        token.SignedFromIp = ipAddress;
        token.UserAgent = userAgent;

        _context.ContractSigningTokens.Update(token);
        await _context.SaveChangesAsync();

        return token;
    }

    public async Task<int> DeleteExpiredTokensAsync()
    {
        var now = DateTime.UtcNow;
        var expiredTokens = await _context.ContractSigningTokens
            .Where(t => t.ExpiresAt < now && !t.IsUsed)
            .ToListAsync();

        if (expiredTokens.Count != 0)
        {
            _context.ContractSigningTokens.RemoveRange(expiredTokens);
            await _context.SaveChangesAsync();
        }

        return expiredTokens.Count;
    }

    public async Task InvalidateTokensByRegistrationIdAsync(Guid registrationId)
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
}
