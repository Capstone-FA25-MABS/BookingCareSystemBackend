using Microsoft.EntityFrameworkCore;
using BookingCare.Services.Payment.Data;
using BookingCare.Services.Payment.Models.Entities;
using BookingCare.Services.Payment.Repositories.Interfaces;
using BookingCare.Shared.Common.Models;

namespace BookingCare.Services.Payment.Repositories.Implementations;

/// <summary>
/// Repository implementation for BankAccount
/// </summary>
public class BankAccountRepository : IBankAccountRepository
{
    private readonly PaymentDbContext _context;

    public BankAccountRepository(PaymentDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get bank account by ID
    /// </summary>
    public async Task<BankAccountEntity?> GetByIdAsync(Guid id)
    {
        return await _context.BankAccounts
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <summary>
    /// Get all bank accounts of a user
    /// </summary>
    public async Task<IEnumerable<BankAccountEntity>> GetByUserIdAsync(Guid userId)
    {
        return await _context.BankAccounts
            .Where(x => x.UserId == userId && x.IsActive)
            .OrderByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get bank accounts of a user with pagination
    /// </summary>
    public async Task<PagedResult<BankAccountEntity>> GetPagedByUserIdAsync(Guid userId, int page, int pageSize, bool? activeOnly = null)
    {
        var query = _context.BankAccounts
            .Where(x => x.UserId == userId);

        if (activeOnly.HasValue && activeOnly.Value)
        {
            query = query.Where(x => x.IsActive);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<BankAccountEntity>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Get the default bank account of a user
    /// </summary>
    public async Task<BankAccountEntity?> GetDefaultByUserIdAsync(Guid userId)
    {
        return await _context.BankAccounts
            .FirstOrDefaultAsync(x => x.UserId == userId && x.IsDefault && x.IsActive);
    }

    /// <summary>
    /// Check if an account number already exists (across users)
    /// </summary>
    public async Task<bool> AccountNumberExistsAsync(string accountNumber, string bankCode, Guid? excludeId = null)
    {
        var query = _context.BankAccounts
            .Where(x => x.AccountNumber == accountNumber && x.BankCode == bankCode);

        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    /// <summary>
    /// Check if an account number already exists for a specific user
    /// </summary>
    public async Task<bool> AccountNumberExistsForUserAsync(string accountNumber, string bankCode, Guid userId, Guid? excludeId = null)
    {
        var query = _context.BankAccounts
            .Where(x => x.AccountNumber == accountNumber && x.BankCode == bankCode && x.UserId == userId);

        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    /// <summary>
    /// Find bank account by accountNumber, bankCode and userId (includes inactive)
    /// </summary>
    public async Task<BankAccountEntity?> FindByAccountNumberAndUserAsync(string accountNumber, string bankCode, Guid userId)
    {
        return await _context.BankAccounts
            .FirstOrDefaultAsync(x => x.AccountNumber == accountNumber &&
                                     x.BankCode == bankCode &&
                                     x.UserId == userId);
    }

    /// <summary>
    /// Check if bank account is used in RefundHistories
    /// </summary>
    public async Task<bool> HasRefundHistoriesAsync(Guid bankAccountId)
    {
        return await _context.RefundHistories
            .AnyAsync(r => r.BankAccountId == bankAccountId);
    }

    /// <summary>
    /// Create a new bank account
    /// </summary>
    public async Task<BankAccountEntity> CreateAsync(BankAccountEntity entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        _context.BankAccounts.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    /// <summary>
    /// Update bank account
    /// </summary>
    public async Task<BankAccountEntity> UpdateAsync(BankAccountEntity entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.BankAccounts.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    /// <summary>
    /// Delete bank account
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null)
            return false;

        _context.BankAccounts.Remove(entity);
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }

    /// <summary>
    /// Set account as default and unset other accounts of the user
    /// </summary>
    public async Task SetAsDefaultAsync(Guid bankAccountId, Guid userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Unset default for all other accounts of the user
            await _context.BankAccounts
                .Where(x => x.UserId == userId && x.Id != bankAccountId)
                .ExecuteUpdateAsync(x => x.SetProperty(p => p.IsDefault, false)
                                         .SetProperty(p => p.UpdatedAt, DateTime.UtcNow));

            // Set the selected account as default
            await _context.BankAccounts
                .Where(x => x.Id == bankAccountId)
                .ExecuteUpdateAsync(x => x.SetProperty(p => p.IsDefault, true)
                                         .SetProperty(p => p.UpdatedAt, DateTime.UtcNow));

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Count bank accounts of a user
    /// </summary>
    public async Task<int> CountByUserIdAsync(Guid userId)
    {
        return await _context.BankAccounts
            .CountAsync(x => x.UserId == userId);
    }
}