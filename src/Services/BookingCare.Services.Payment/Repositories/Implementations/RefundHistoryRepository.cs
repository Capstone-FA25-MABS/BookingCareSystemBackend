using Microsoft.EntityFrameworkCore;
using BookingCare.Services.Payment.Data;
using BookingCare.Services.Payment.Models.Entities;
using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Services.Payment.Repositories.Interfaces;
using BookingCare.Services.Payment.Enums;
using BookingCare.Shared.Common.Models;

namespace BookingCare.Services.Payment.Repositories.Implementations;

/// <summary>
/// Repository implementation cho RefundHistory
/// </summary>
public class RefundHistoryRepository : IRefundHistoryRepository
{
    private readonly PaymentDbContext _context;

    public RefundHistoryRepository(PaymentDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// L?y refund history theo ID
    /// </summary>
    public async Task<RefundHistoryEntity?> GetByIdAsync(Guid id)
    {
        return await _context.RefundHistories
            .Include(r => r.Payment)
                .ThenInclude(p => p.PaymentMethod)
            .Include(r => r.BankAccount)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    /// <summary>
    /// L?y refund history theo payment ID
    /// </summary>
    public async Task<RefundHistoryEntity?> GetByPaymentIdAsync(Guid paymentId)
    {
        return await _context.RefundHistories
            .Include(r => r.Payment)
                .ThenInclude(p => p.PaymentMethod)
            .Include(r => r.BankAccount)
            .FirstOrDefaultAsync(r => r.PaymentId == paymentId);
    }

    /// <summary>
    /// L?y danh sách refund histories theo user ID
    /// </summary>
    public async Task<IEnumerable<RefundHistoryEntity>> GetByUserIdAsync(Guid userId)
    {
        return await _context.RefundHistories
            .Include(r => r.Payment)
                .ThenInclude(p => p.PaymentMethod)
            .Include(r => r.BankAccount)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// L?y danh sách refund histories theo user ID ch? v?i status PENDING và COMPLETED
    /// </summary>
    public async Task<IEnumerable<RefundHistoryEntity>> GetProcessableRefundsByUserIdAsync(Guid userId)
    {
        return await _context.RefundHistories
            .Include(r => r.Payment)
                .ThenInclude(p => p.PaymentMethod)
            .Include(r => r.BankAccount)
            .Where(r => r.UserId == userId &&
                       (r.Status == RefundStatus.PENDING || r.Status == RefundStatus.COMPLETED))
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// L?y danh sách refund histories theo tr?ng thái
    /// </summary>
    public async Task<IEnumerable<RefundHistoryEntity>> GetByStatusAsync(RefundStatus status)
    {
        return await _context.RefundHistories
            .Include(r => r.Payment)
                .ThenInclude(p => p.PaymentMethod)
            .Include(r => r.BankAccount)
            .Where(r => r.Status == status)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// L?y danh sách refund histories v?i phân trang và filter
    /// </summary>
    public async Task<PagedResult<RefundHistoryEntity>> GetPagedAsync(GetRefundHistoriesRequest request)
    {
        var query = _context.RefundHistories
            .Include(r => r.Payment)
                .ThenInclude(p => p.PaymentMethod)
            .Include(r => r.BankAccount)
            .AsQueryable();

        // Apply filters
        if (request.UserId.HasValue)
        {
            query = query.Where(r => r.UserId == request.UserId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(r => r.Status == request.Status.Value);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt <= request.ToDate.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return new PagedResult<RefundHistoryEntity>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.Page,
            PageSize = request.PageSize
        };
    }

    /// <summary>
    /// Ki?m tra payment ?ã có refund history ch?a
    /// </summary>
    public async Task<bool> PaymentHasRefundAsync(Guid paymentId)
    {
        return await _context.RefundHistories
            .AnyAsync(r => r.PaymentId == paymentId);
    }

    /// <summary>
    /// ??m s? l??ng refund histories theo tr?ng thái
    /// </summary>
    public async Task<int> CountByStatusAsync(RefundStatus status)
    {
        return await _context.RefundHistories
            .CountAsync(r => r.Status == status);
    }

    /// <summary>
    /// ??m s? l??ng refund histories c?a user
    /// </summary>
    public async Task<int> CountByUserIdAsync(Guid userId)
    {
        return await _context.RefundHistories
            .CountAsync(r => r.UserId == userId);
    }

    /// <summary>
    /// T?o refund history m?i
    /// </summary>
    public async Task<RefundHistoryEntity> CreateAsync(RefundHistoryEntity entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        _context.RefundHistories.Add(entity);
        await _context.SaveChangesAsync();

        // Load related entities
        return await GetByIdAsync(entity.Id) ?? entity;
    }

    /// <summary>
    /// C?p nh?t refund history
    /// </summary>
    public async Task<RefundHistoryEntity> UpdateAsync(RefundHistoryEntity entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;

        _context.RefundHistories.Update(entity);
        await _context.SaveChangesAsync();

        // Load related entities
        return await GetByIdAsync(entity.Id) ?? entity;
    }

    /// <summary>
    /// Xóa refund history
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _context.RefundHistories.FindAsync(id);
        if (entity == null)
            return false;

        _context.RefundHistories.Remove(entity);
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }

    /// <summary>
    /// L?y danh sách refund histories c?n x? lý (WAITING -> PENDING khi user có bank account)
    /// </summary>
    public async Task<IEnumerable<RefundHistoryEntity>> GetPendingProcessAsync()
    {
        return await _context.RefundHistories
            .Include(r => r.Payment)
            .Include(r => r.BankAccount)
            .Where(r => r.Status == RefundStatus.WAITING)
            .Where(r => _context.BankAccounts.Any(b => b.UserId == r.UserId && b.IsActive))
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();
    }
}