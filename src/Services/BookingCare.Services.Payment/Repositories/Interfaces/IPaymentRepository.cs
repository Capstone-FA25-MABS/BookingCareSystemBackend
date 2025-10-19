using BookingCare.Services.Payment.Models.Entities;
using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Shared.Common.Models;

namespace BookingCare.Services.Payment.Repositories.Interfaces;

/// <summary>
/// Interface for Payment Repository
/// </summary>
public interface IPaymentRepository
{
    /// <summary>
    /// Get payment by ID
    /// </summary>
    Task<PaymentEntity?> GetByIdAsync(Guid id);

    /// <summary>
    /// Get payment by appointment ID
    /// </summary>
    Task<PaymentEntity?> GetByAppointmentIdAsync(Guid appointmentId);

    /// <summary>
    /// Get payment by subscription ID
    /// </summary>
    Task<PaymentEntity?> GetBySubscriptionIdAsync(Guid subscriptionId);

    /// <summary>
    /// Get list of payments by hospital ID
    /// </summary>
    Task<IEnumerable<PaymentEntity>> GetByHospitalIdAsync(Guid hospitalId);

    /// <summary>
    /// Get paged list of payments by hospital ID
    /// </summary>
    Task<PagedResult<PaymentEntity>> GetPagedByHospitalIdAsync(Guid hospitalId, GetPaymentsPagedRequest request);

    /// <summary>
    /// Get list of payments by patient ID
    /// </summary>
    Task<IEnumerable<PaymentEntity>> GetByPatientIdAsync(Guid patientId);

    /// <summary>
    /// Get paged list of payments by patient ID
    /// </summary>
    Task<PagedResult<PaymentEntity>> GetPagedByPatientIdAsync(Guid patientId, GetPaymentsPagedRequest request);

    /// <summary>
    /// Get payment statistics data
    /// </summary>
    Task<IEnumerable<PaymentEntity>> GetPaymentStatisticsAsync(GetPaymentStatisticsRequest request);

    /// <summary>
    /// Create a new payment
    /// </summary>
    Task<PaymentEntity> CreateAsync(PaymentEntity payment);

    /// <summary>
    /// Update payment
    /// </summary>
    Task<PaymentEntity> UpdateAsync(PaymentEntity payment);

    /// <summary>
    /// Delete payment
    /// </summary>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// Check if payment exists
    /// </summary>
    Task<bool> ExistsAsync(Guid id);
}