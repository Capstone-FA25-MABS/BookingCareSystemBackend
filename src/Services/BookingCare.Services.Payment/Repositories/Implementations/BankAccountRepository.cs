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
    /// Lấy bank account theo ID
    /// </summary>
    public async Task<BankAccountEntity?> GetByIdAsync(Guid id)
    {
        return await _context.BankAccounts
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <summary>
    /// Lấy tất cả bank accounts của user
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
    /// Lấy bank accounts của user với phân trang
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
    /// Lấy bank account mặc định của user
    /// </summary>
    public async Task<BankAccountEntity?> GetDefaultByUserIdAsync(Guid userId)
    {
        return await _context.BankAccounts
            .FirstOrDefaultAsync(x => x.UserId == userId && x.IsDefault && x.IsActive);
    }

    /// <summary>
    /// Kiểm tra số tài khoản đã tồn tại chưa (không phân biệt user)
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
    /// Kiểm tra số tài khoản đã tồn tại của user cụ thể chưa
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
    /// Tìm bank account theo accountNumber, bankCode và userId (bao gồm cả inactive)
    /// </summary>
    public async Task<BankAccountEntity?> FindByAccountNumberAndUserAsync(string accountNumber, string bankCode, Guid userId)
    {
        return await _context.BankAccounts
            .FirstOrDefaultAsync(x => x.AccountNumber == accountNumber &&
                                     x.BankCode == bankCode &&
                                     x.UserId == userId);
    }

    /// <summary>
    /// Kiểm tra bank account có được sử dụng trong RefundHistories không
    /// </summary>
    public async Task<bool> HasRefundHistoriesAsync(Guid bankAccountId)
    {
        return await _context.RefundHistories
            .AnyAsync(r => r.BankAccountId == bankAccountId);
    }

    /// <summary>
    /// Tạo bank account mới
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
    /// Cập nhật bank account
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
    /// Đặt tài khoản làm mặc định và bỏ mặc định các tài khoản khác của user
    /// </summary>
    public async Task SetAsDefaultAsync(Guid bankAccountId, Guid userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Bỏ mặc định tất cả tài khoản khác của user
            await _context.BankAccounts
                .Where(x => x.UserId == userId && x.Id != bankAccountId)
                .ExecuteUpdateAsync(x => x.SetProperty(p => p.IsDefault, false)
                                         .SetProperty(p => p.UpdatedAt, DateTime.UtcNow));

            // Đặt tài khoản được chọn làm mặc định
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
    /// Đếm số lượng bank accounts của user
    /// </summary>
    public async Task<int> CountByUserIdAsync(Guid userId)
    {
        return await _context.BankAccounts
            .CountAsync(x => x.UserId == userId);
    }
}