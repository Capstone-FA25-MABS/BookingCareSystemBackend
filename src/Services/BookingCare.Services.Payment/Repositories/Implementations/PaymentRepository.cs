using Microsoft.EntityFrameworkCore;
using BookingCare.Services.Payment.Data;
using BookingCare.Services.Payment.Models.Entities;
using BookingCare.Services.Payment.Repositories.Interfaces;

namespace BookingCare.Services.Payment.Repositories.Implementations;

/// <summary>
/// Implementation c?a Payment Repository
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

    public async Task<IEnumerable<PaymentEntity>> GetByPatientIdAsync(Guid patientId)
    {
        return await _context.Payments
            .Include(p => p.PaymentMethod)
            .Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
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
}