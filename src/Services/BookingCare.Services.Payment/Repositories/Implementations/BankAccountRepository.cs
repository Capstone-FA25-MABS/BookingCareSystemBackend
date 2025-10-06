using Microsoft.EntityFrameworkCore;
using BookingCare.Services.Payment.Data;
using BookingCare.Services.Payment.Models.Entities;
using BookingCare.Services.Payment.Repositories.Interfaces;
using BookingCare.Shared.Common.Models;

namespace BookingCare.Services.Payment.Repositories.Implementations;

/// <summary>
/// Repository implementation cho BankAccount
/// </summary>
public class BankAccountRepository : IBankAccountRepository
{
    private readonly PaymentDbContext _context;

    public BankAccountRepository(PaymentDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// L?y bank account theo ID
    /// </summary>
    public async Task<BankAccountEntity?> GetByIdAsync(Guid id)
    {
        return await _context.BankAccounts
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <summary>
    /// L?y t?t c? bank accounts c?a user
    /// </summary>
    public async Task<IEnumerable<BankAccountEntity>> GetByUserIdAsync(Guid userId)
    {
        return await _context.BankAccounts
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// L?y bank accounts c?a user v?i phân trang
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
    /// L?y bank account m?c ??nh c?a user
    /// </summary>
    public async Task<BankAccountEntity?> GetDefaultByUserIdAsync(Guid userId)
    {
        return await _context.BankAccounts
            .FirstOrDefaultAsync(x => x.UserId == userId && x.IsDefault && x.IsActive);
    }

    /// <summary>
    /// Ki?m tra s? tài kho?n ?ã t?n t?i ch?a
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
    /// T?o bank account m?i
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
    /// C?p nh?t bank account
    /// </summary>
    public async Task<BankAccountEntity> UpdateAsync(BankAccountEntity entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.BankAccounts.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    /// <summary>
    /// Xóa bank account
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
    /// ??t tài kho?n làm m?c ??nh và b? m?c ??nh các tài kho?n khác c?a user
    /// </summary>
    public async Task SetAsDefaultAsync(Guid bankAccountId, Guid userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // B? m?c ??nh t?t c? tài kho?n khác c?a user
            await _context.BankAccounts
                .Where(x => x.UserId == userId && x.Id != bankAccountId)
                .ExecuteUpdateAsync(x => x.SetProperty(p => p.IsDefault, false)
                                         .SetProperty(p => p.UpdatedAt, DateTime.UtcNow));

            // ??t tài kho?n ???c ch?n làm m?c ??nh
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
    /// ??m s? l??ng bank accounts c?a user
    /// </summary>
    public async Task<int> CountByUserIdAsync(Guid userId)
    {
        return await _context.BankAccounts
            .CountAsync(x => x.UserId == userId);
    }
}