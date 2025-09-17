using Microsoft.EntityFrameworkCore;
using BookingCare.Services.Payment.Data;
using BookingCare.Services.Payment.Models.Entities;
using BookingCare.Services.Payment.Repositories.Interfaces;
using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Shared.Common.Models;
using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Payment.Repositories.Implementations;

/// <summary>
/// Implementation của Payment Repository
/// </summary>
public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _context;

    public PaymentRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<PaymentEntity?> GetByIdAsync(Guid id)
    {
        return await _context.Payments
            .Include(p => p.PaymentMethod)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<PaymentEntity?> GetByAppointmentIdAsync(Guid appointmentId)
    {
        return await _context.Payments
            .Include(p => p.PaymentMethod)
            .FirstOrDefaultAsync(p => p.AppointmentId == appointmentId);
    }

    public async Task<PaymentEntity?> GetBySubscriptionIdAsync(Guid subscriptionId)
    {
        return await _context.Payments
            .Include(p => p.PaymentMethod)
            .FirstOrDefaultAsync(p => p.SubscriptionId == subscriptionId);
    }

    public async Task<IEnumerable<PaymentEntity>> GetByClinicIdAsync(Guid clinicId)
    {
        return await _context.Payments
            .Include(p => p.PaymentMethod)
            .Where(p => p.ClinicId == clinicId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<PagedResult<PaymentEntity>> GetPagedByClinicIdAsync(Guid clinicId, GetPaymentsPagedRequest request)
    {
        var query = _context.Payments
            .Include(p => p.PaymentMethod)
            .Where(p => p.ClinicId == clinicId);

        // Apply search filter if provided
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(p =>
                p.PaymentMethod.Name.Contains(request.SearchTerm) ||
                p.Amount.ToString().Contains(request.SearchTerm) ||
                p.Status.ToString().Contains(request.SearchTerm));
        }

        // Apply sorting
        query = ApplySorting(query, request.SortBy, request.SortOrder);

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply pagination
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return new PagedResult<PaymentEntity>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    public async Task<IEnumerable<PaymentEntity>> GetByPatientIdAsync(Guid patientId)
    {
        return await _context.Payments
            .Include(p => p.PaymentMethod)
            .Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<PagedResult<PaymentEntity>> GetPagedByPatientIdAsync(Guid patientId, GetPaymentsPagedRequest request)
    {
        var query = _context.Payments
            .Include(p => p.PaymentMethod)
            .Where(p => p.PatientId == patientId);

        // Apply search filter if provided
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(p =>
                p.PaymentMethod.Name.Contains(request.SearchTerm) ||
                p.Amount.ToString().Contains(request.SearchTerm) ||
                p.Status.ToString().Contains(request.SearchTerm));
        }

        // Apply sorting
        query = ApplySorting(query, request.SortBy, request.SortOrder);

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply pagination
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return new PagedResult<PaymentEntity>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    private static IQueryable<PaymentEntity> ApplySorting(IQueryable<PaymentEntity> query, string sortBy, string sortOrder)
    {
        var isDescending = sortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase);

        return sortBy.ToLower() switch
        {
            "amount" => isDescending ? query.OrderByDescending(p => p.Amount) : query.OrderBy(p => p.Amount),
            "status" => isDescending ? query.OrderByDescending(p => p.Status) : query.OrderBy(p => p.Status),
            "createdat" => isDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };
    }

    public async Task<PaymentEntity> CreateAsync(PaymentEntity payment)
    {
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        // Reload v?i PaymentMethod
        return await GetByIdAsync(payment.Id) ?? payment;
    }

    public async Task<PaymentEntity> UpdateAsync(PaymentEntity payment)
    {
        _context.Payments.Update(payment);
        await _context.SaveChangesAsync();

        // Reload v?i PaymentMethod
        return await GetByIdAsync(payment.Id) ?? payment;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var payment = await _context.Payments.FindAsync(id);
        if (payment == null)
            return false;

        _context.Payments.Remove(payment);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Payments.AnyAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<PaymentEntity>> GetPaymentStatisticsAsync(GetPaymentStatisticsRequest request)
    {
        // S? d?ng computed dates v?i default values
        var fromDate = request.GetFromDate();
        var toDate = request.GetToDate();

        var query = _context.Payments
            .Include(p => p.PaymentMethod)
            .Where(p => p.CreatedAt >= fromDate && p.CreatedAt <= toDate);

        // Apply filters
        if (request.ClinicId.HasValue)
        {
            query = query.Where(p => p.ClinicId == request.ClinicId.Value);
        }

        if (request.PatientId.HasValue)
        {
            query = query.Where(p => p.PatientId == request.PatientId.Value);
        }

        if (!string.IsNullOrEmpty(request.TransactionType))
        {
            if (Enum.TryParse<TransactionType>(request.TransactionType, true, out var transactionType))
            {
                query = query.Where(p => p.TransactionType == transactionType);
            }
        }

        if (!string.IsNullOrEmpty(request.Status))
        {
            if (Enum.TryParse<PaymentStatus>(request.Status, true, out var status))
            {
                query = query.Where(p => p.Status == status);
            }
        }

        return await query
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();
    }
}