using BookingCare.Services.Payment.Data;
using BookingCare.Services.Payment.Enums;
using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Services.Payment.Models.Entities;
using BookingCare.Services.Payment.Repositories.Interfaces;
using BookingCare.Shared.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.Payment.Repositories.Implementations;

/// <summary>
/// Implementation của Hospital Payout Repository
/// </summary>
public class HospitalPayoutRepository : IHospitalPayoutRepository
{
    private readonly PaymentDbContext _context;

    public HospitalPayoutRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<HospitalPayoutEntity?> GetByIdAsync(Guid id)
    {
        return await _context
            .HospitalPayouts.Include(p => p.BankAccount)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<PagedResult<HospitalPayoutEntity>> GetPayoutsAsync(PayoutQueryRequest query)
    {
        var queryable = _context.HospitalPayouts.Include(p => p.BankAccount).AsQueryable();

        // Filter by hospital ID
        if (query.HospitalId.HasValue)
        {
            queryable = queryable.Where(p => p.HospitalId == query.HospitalId.Value);
        }

        // Filter by hospital name (case-insensitive partial match)
        if (!string.IsNullOrWhiteSpace(query.HospitalName))
        {
            queryable = queryable.Where(p => p.HospitalName.Contains(query.HospitalName));
        }

        // Filter by status
        if (
            !string.IsNullOrWhiteSpace(query.Status)
            && Enum.TryParse<PayoutStatus>(query.Status, true, out var status)
        )
        {
            queryable = queryable.Where(p => p.Status == status);
        }

        // Filter by period (month/year format: "2024-11")
        if (!string.IsNullOrWhiteSpace(query.Period))
        {
            var parts = query.Period.Split('-');
            if (
                parts.Length == 2
                && int.TryParse(parts[0], out var year)
                && int.TryParse(parts[1], out var month)
            )
            {
                var periodStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
                var periodEnd = periodStart.AddMonths(1).AddDays(-1);
                queryable = queryable.Where(p =>
                    p.PeriodStart >= periodStart && p.PeriodEnd <= periodEnd
                );
            }
        }

        // Order by creation date descending (newest first)
        queryable = queryable.OrderByDescending(p => p.CreatedAt);

        // Get total count
        var totalCount = await queryable.CountAsync();

        // Apply pagination
        var items = await queryable
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PagedResult<HospitalPayoutEntity>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
        };
    }

    public async Task<List<HospitalPayoutEntity>> GetByHospitalIdAsync(Guid hospitalId)
    {
        return await _context
            .HospitalPayouts.Include(p => p.BankAccount)
            .Where(p => p.HospitalId == hospitalId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> ExistsForPeriodAsync(
        Guid hospitalId,
        DateTime periodStart,
        DateTime periodEnd
    )
    {
        return await _context.HospitalPayouts.AnyAsync(p =>
            p.HospitalId == hospitalId && p.PeriodStart == periodStart && p.PeriodEnd == periodEnd
        );
    }

    public async Task<HospitalPayoutEntity> CreateAsync(HospitalPayoutEntity payout)
    {
        _context.HospitalPayouts.Add(payout);
        await _context.SaveChangesAsync();
        return payout;
    }

    public async Task<HospitalPayoutEntity> UpdateAsync(HospitalPayoutEntity payout)
    {
        payout.UpdatedAt = DateTime.UtcNow;
        _context.HospitalPayouts.Update(payout);
        await _context.SaveChangesAsync();
        return payout;
    }

    public async Task<decimal> GetTotalPendingAmountAsync(Guid? hospitalId = null)
    {
        var query = _context.HospitalPayouts.Where(p => p.Status == PayoutStatus.PENDING);

        if (hospitalId.HasValue)
        {
            query = query.Where(p => p.HospitalId == hospitalId.Value);
        }

        return await query.SumAsync(p => p.TotalAmount);
    }

    public async Task<(
        int PendingCount,
        decimal PendingAmount,
        int CompletedCount,
        decimal CompletedAmount
    )> GetStatisticsAsync()
    {
        var pending = await _context
            .HospitalPayouts.Where(p => p.Status == PayoutStatus.PENDING)
            .GroupBy(p => 1)
            .Select(g => new { Count = g.Count(), Amount = g.Sum(p => p.TotalAmount) })
            .FirstOrDefaultAsync();

        var completed = await _context
            .HospitalPayouts.Where(p => p.Status == PayoutStatus.COMPLETED)
            .GroupBy(p => 1)
            .Select(g => new { Count = g.Count(), Amount = g.Sum(p => p.TotalAmount) })
            .FirstOrDefaultAsync();

        return (
            pending?.Count ?? 0,
            pending?.Amount ?? 0,
            completed?.Count ?? 0,
            completed?.Amount ?? 0
        );
    }
}
