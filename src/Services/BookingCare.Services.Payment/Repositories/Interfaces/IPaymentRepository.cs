using BookingCare.Services.Payment.Models.Entities;

namespace BookingCare.Services.Payment.Repositories.Interfaces;

/// <summary>
/// Interface cho Payment Repository
/// </summary>
public interface IPaymentRepository
{
    /// <summary>
    /// L?y payment theo ID
    /// </summary>
    Task<PaymentEntity?> GetByIdAsync(Guid id);

    /// <summary>
    /// L?y payment theo appointment ID
    /// </summary>
    Task<PaymentEntity?> GetByAppointmentIdAsync(Guid appointmentId);

    /// <summary>
    /// L?y payment theo subscription ID
    /// </summary>
    Task<PaymentEntity?> GetBySubscriptionIdAsync(Guid subscriptionId);

    /// <summary>
    /// L?y danh sách payments theo clinic ID
    /// </summary>
    Task<IEnumerable<PaymentEntity>> GetByClinicIdAsync(Guid clinicId);

    /// <summary>
    /// L?y danh sách payments theo patient ID
    /// </summary>
    Task<IEnumerable<PaymentEntity>> GetByPatientIdAsync(Guid patientId);

    /// <summary>
    /// T?o payment m?i
    /// </summary>
    Task<PaymentEntity> CreateAsync(PaymentEntity payment);

    /// <summary>
    /// C?p nh?t payment
    /// </summary>
    Task<PaymentEntity> UpdateAsync(PaymentEntity payment);

    /// <summary>
    /// Xóa payment
    /// </summary>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// Ki?m tra payment có t?n t?i không
    /// </summary>
    Task<bool> ExistsAsync(Guid id);
}