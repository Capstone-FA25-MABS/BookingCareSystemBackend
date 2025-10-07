using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Services.Payment.Models.DTOs.Responses;
using BookingCare.Shared.Common.Models;

namespace BookingCare.Services.Payment.Services.Interfaces;

/// <summary>
/// Interface for Payment Service
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Get payment by ID
    /// </summary>
    Task<PaymentResponse?> GetByIdAsync(Guid id);

    /// <summary>
    /// Get payment by appointment ID
    /// </summary>
    Task<PaymentResponse?> GetByAppointmentIdAsync(Guid appointmentId);

    /// <summary>
    /// Get payment by subscription ID
    /// </summary>
    Task<PaymentResponse?> GetBySubscriptionIdAsync(Guid subscriptionId);

    /// <summary>
    /// Get list of payments by clinic ID
    /// </summary>
    Task<IEnumerable<PaymentResponse>> GetByClinicIdAsync(Guid clinicId);

    /// <summary>
    /// Get list of payments by clinic ID with pagination
    /// </summary>
    Task<PagedResult<PaymentResponse>> GetPagedByClinicIdAsync(Guid clinicId, GetPaymentsPagedRequest request);

    /// <summary>
    /// Get list of payments by patient ID
    /// </summary>
    Task<IEnumerable<PaymentResponse>> GetByPatientIdAsync(Guid patientId);

    /// <summary>
    /// Get list of payments by patient ID with pagination
    /// </summary>
    Task<PagedResult<PaymentResponse>> GetPagedByPatientIdAsync(Guid patientId, GetPaymentsPagedRequest request);

    /// <summary>
    /// Get payment statistics
    /// </summary>
    Task<PaymentStatisticsResponse> GetPaymentStatisticsAsync(GetPaymentStatisticsRequest request);

    /// <summary>
    /// Create new payment (generic - deprecated, use CreateAppointmentPaymentAsync or CreateSubscriptionPaymentAsync)
    /// </summary>
    Task<PaymentResponse> CreateAsync(CreatePaymentRequest request);

    /// <summary>
    /// Create payment for appointment (patient books appointment)
    /// </summary>
    Task<PaymentResponse> CreateAppointmentPaymentAsync(CreateAppointmentPaymentRequest request);

    /// <summary>
    /// Create payment for subscription (clinic subscribes to package)
    /// </summary>
    Task<PaymentResponse> CreateSubscriptionPaymentAsync(CreateSubscriptionPaymentRequest request);

    /// <summary>
    /// Update payment status
    /// </summary>
    Task<PaymentResponse> UpdateStatusAsync(UpdatePaymentStatusRequest request);

    /// <summary>
    /// Delete payment
    /// </summary>
    Task<bool> DeleteAsync(Guid id);
}