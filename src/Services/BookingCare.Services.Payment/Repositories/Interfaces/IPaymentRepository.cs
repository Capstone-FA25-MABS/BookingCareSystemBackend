using BookingCare.Services.Payment.Models.Entities;
using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Shared.Common.Models;

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
    /// L?y danh s?ch payments theo clinic ID
    /// </summary>
    Task<IEnumerable<PaymentEntity>> GetByClinicIdAsync(Guid clinicId);

    /// <summary>
    /// L?y danh s?ch payments theo clinic ID v?i phân trang
    /// </summary>
    Task<PagedResult<PaymentEntity>> GetPagedByClinicIdAsync(Guid clinicId, GetPaymentsPagedRequest request);

    /// <summary>
    /// L?y danh s?ch payments theo patient ID
    /// </summary>
    Task<IEnumerable<PaymentEntity>> GetByPatientIdAsync(Guid patientId);

    /// <summary>
    /// L?y danh s?ch payments theo patient ID v?i phân trang
    /// </summary>
    Task<PagedResult<PaymentEntity>> GetPagedByPatientIdAsync(Guid patientId, GetPaymentsPagedRequest request);

    /// <summary>
    /// L?y d? li?u th?ng kê payments
    /// </summary>
    Task<IEnumerable<PaymentEntity>> GetPaymentStatisticsAsync(GetPaymentStatisticsRequest request);

    /// <summary>
    /// T?o payment m?i
    /// </summary>
    Task<PaymentEntity> CreateAsync(PaymentEntity payment);

    /// <summary>
    /// C?p nh?t payment
    /// </summary>
    Task<PaymentEntity> UpdateAsync(PaymentEntity payment);

    /// <summary>
    /// X?a payment
    /// </summary>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// Ki?m tra payment c? t?n t?i kh?ng
    /// </summary>
    Task<bool> ExistsAsync(Guid id);
}