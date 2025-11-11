using BookingCare.Services.Payment.Data;
using BookingCare.Services.Payment.Models.Entities;
using BookingCare.Services.Payment.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.Payment.Repositories.Implementations;

/// <summary>
/// Implementation of PayOS Payment Mapping Repository
/// </summary>
public class PayOSPaymentMappingRepository : IPayOSPaymentMappingRepository
{
    private readonly PaymentDbContext _context;

    public PayOSPaymentMappingRepository(PaymentDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Create a new mapping between PaymentId and OrderCode
    /// </summary>
    public async Task<PayOSPaymentMappingEntity> CreateMappingAsync(
        Guid paymentId,
        long orderCode,
        DateTime? expiresAt = null
    )
    {
        var mapping = new PayOSPaymentMappingEntity(paymentId, orderCode, expiresAt);

        _context.PayOSPaymentMappings.Add(mapping);
        await _context.SaveChangesAsync();

        return mapping;
    }

    /// <summary>
    /// Create a new mapping for subscription payment
    /// </summary>
    public async Task<PayOSPaymentMappingEntity> CreateSubscriptionMappingAsync(
        Guid paymentId,
        long orderCode,
        Guid subscriptionPlanId,
        Guid hospitalId,
        bool? isUpgrade,
        Guid? currentHospitalSubscriptionId,
        string? planType,
        DateTime? expiresAt
    )
    {
        var mapping = new PayOSPaymentMappingEntity(paymentId, orderCode, expiresAt)
        {
            SubscriptionPlanId = subscriptionPlanId,
            HospitalId = hospitalId,
            IsSubscriptionUpgrade = isUpgrade,
            CurrentHospitalSubscriptionId = currentHospitalSubscriptionId,
            PlanType = planType,
        };

        _context.PayOSPaymentMappings.Add(mapping);
        await _context.SaveChangesAsync();

        return mapping;
    }

    /// <summary>
    /// Get PaymentId by OrderCode
    /// </summary>
    public async Task<Guid?> GetPaymentIdByOrderCodeAsync(long orderCode)
    {
        var mapping = await _context
            .PayOSPaymentMappings.AsNoTracking()
            .FirstOrDefaultAsync(m => m.OrderCode == orderCode);

        return mapping?.PaymentId;
    }

    /// <summary>
    /// Get OrderCode by PaymentId
    /// </summary>
    public async Task<long?> GetOrderCodeByPaymentIdAsync(Guid paymentId)
    {
        var mapping = await _context
            .PayOSPaymentMappings.AsNoTracking()
            .FirstOrDefaultAsync(m => m.PaymentId == paymentId);

        return mapping?.OrderCode;
    }

    /// <summary>
    /// Get mapping by OrderCode
    /// </summary>
    public async Task<PayOSPaymentMappingEntity?> GetMappingByOrderCodeAsync(long orderCode)
    {
        return await _context
            .PayOSPaymentMappings.AsNoTracking()
            .FirstOrDefaultAsync(m => m.OrderCode == orderCode);
    }

    /// <summary>
    /// Delete mapping by OrderCode (after payment completed)
    /// </summary>
    public async Task<bool> DeleteMappingAsync(long orderCode)
    {
        var mapping = await _context.PayOSPaymentMappings.FirstOrDefaultAsync(m =>
            m.OrderCode == orderCode
        );

        if (mapping == null)
            return false;

        _context.PayOSPaymentMappings.Remove(mapping);
        var deleted = await _context.SaveChangesAsync();

        return deleted > 0;
    }

    /// <summary>
    /// Delete expired mappings (cleanup job)
    /// </summary>
    public async Task<int> CleanupExpiredMappingsAsync()
    {
        var now = DateTime.UtcNow;
        var expiredMappings = await _context
            .PayOSPaymentMappings.Where(m => m.ExpiresAt < now)
            .ToListAsync();

        if (expiredMappings.Count == 0)
            return 0;

        _context.PayOSPaymentMappings.RemoveRange(expiredMappings);
        await _context.SaveChangesAsync();

        return expiredMappings.Count;
    }

    /// <summary>
    /// Check if a mapping exists
    /// </summary>
    public async Task<bool> MappingExistsAsync(long orderCode)
    {
        return await _context
            .PayOSPaymentMappings.AsNoTracking()
            .AnyAsync(m => m.OrderCode == orderCode);
    }
}
