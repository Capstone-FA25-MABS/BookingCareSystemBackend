using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Services.Payment.Models.DTOs.Responses;
using BookingCare.Shared.Common.Models;

namespace BookingCare.Services.Payment.Services.Interfaces;

/// <summary>
/// Interface cho Payment Service
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// L?y payment theo ID
    /// </summary>
    Task<PaymentResponse?> GetByIdAsync(Guid id);

    /// <summary>
    /// L?y payment theo appointment ID
    /// </summary>
    Task<PaymentResponse?> GetByAppointmentIdAsync(Guid appointmentId);

    /// <summary>
    /// L?y payment theo subscription ID
    /// </summary>
    Task<PaymentResponse?> GetBySubscriptionIdAsync(Guid subscriptionId);

    /// <summary>
    /// L?y danh s?ch payments theo clinic ID
    /// </summary>
    Task<IEnumerable<PaymentResponse>> GetByClinicIdAsync(Guid clinicId);

    /// <summary>
    /// L?y danh s?ch payments theo clinic ID v?i phân trang
    /// </summary>
    Task<PagedResult<PaymentResponse>> GetPagedByClinicIdAsync(Guid clinicId, GetPaymentsPagedRequest request);

    /// <summary>
    /// L?y danh s?ch payments theo patient ID
    /// </summary>
    Task<IEnumerable<PaymentResponse>> GetByPatientIdAsync(Guid patientId);

    /// <summary>
    /// L?y danh s?ch payments theo patient ID v?i phân trang
    /// </summary>
    Task<PagedResult<PaymentResponse>> GetPagedByPatientIdAsync(Guid patientId, GetPaymentsPagedRequest request);

    /// <summary>
    /// L?y th?ng kê payments
    /// </summary>
    Task<PaymentStatisticsResponse> GetPaymentStatisticsAsync(GetPaymentStatisticsRequest request);

    /// <summary>
    /// T?o payment m?i (generic - deprecated, nên dùng CreateAppointmentPaymentAsync ho?c CreateSubscriptionPaymentAsync)
    /// </summary>
    Task<PaymentResponse> CreateAsync(CreatePaymentRequest request);

    /// <summary>
    /// T?o payment cho appointment (patient ??t l?ch)
    /// </summary>
    Task<PaymentResponse> CreateAppointmentPaymentAsync(CreateAppointmentPaymentRequest request);

    /// <summary>
    /// T?o payment cho subscription (clinic ??ng ký gói)
    /// </summary>
    Task<PaymentResponse> CreateSubscriptionPaymentAsync(CreateSubscriptionPaymentRequest request);

    /// <summary>
    /// C?p nh?t tr?ng th?i payment
    /// </summary>
    Task<PaymentResponse> UpdateStatusAsync(UpdatePaymentStatusRequest request);

    /// <summary>
    /// X?a payment
    /// </summary>
    Task<bool> DeleteAsync(Guid id);
}