using Microsoft.EntityFrameworkCore;
using BookingCare.Services.Payment.Data;
using BookingCare.Services.Payment.Models.Entities;
using BookingCare.Services.Payment.Repositories.Interfaces;
using BookingCare.Services.Payment.Enums;

namespace BookingCare.Services.Payment.Repositories.Implementations;

/// <summary>
/// Implementation của PaymentMethod Repository
/// </summary>
public class PaymentMethodRepository : IPaymentMethodRepository
{
    private readonly PaymentDbContext _context;

    public PaymentMethodRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<PaymentMethodEntity?> GetByIdAsync(Guid id)
    {
        return await _context.PaymentMethods.FindAsync(id);
    }

    public async Task<IEnumerable<PaymentMethodEntity>> GetAllAsync()
    {
        return await _context.PaymentMethods
            .OrderBy(pm => pm.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<PaymentMethodEntity>> GetActiveAsync()
    {
        return await _context.PaymentMethods
            .Where(pm => pm.Status == PaymentMethodStatus.ACTIVE)
            .OrderBy(pm => pm.Name)
            .ToListAsync();
    }

    public async Task<PaymentMethodEntity?> GetByNameAsync(string name)
    {
        return await _context.PaymentMethods
            .FirstOrDefaultAsync(pm => pm.Name == name);
    }

    public async Task<PaymentMethodEntity> UpdateAsync(PaymentMethodEntity paymentMethod)
    {
        _context.PaymentMethods.Update(paymentMethod);
        await _context.SaveChangesAsync();
        return paymentMethod;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.PaymentMethods.AnyAsync(pm => pm.Id == id);
    }
}