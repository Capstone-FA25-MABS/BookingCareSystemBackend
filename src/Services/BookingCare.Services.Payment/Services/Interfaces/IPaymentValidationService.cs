using FluentValidation;
using BookingCare.Services.Payment.Models.DTOs.Requests;

namespace BookingCare.Services.Payment.Services.Interfaces;

/// <summary>
/// Service interface for payment-related validation operations
/// </summary>
public interface IPaymentValidationService
{
    /// <summary>
    /// Validate create appointment payment request
    /// </summary>
    Task<FluentValidation.Results.ValidationResult> ValidateCreateAppointmentPaymentAsync(CreateAppointmentPaymentRequest request);

    /// <summary>
    /// Validate create subscription payment request
    /// </summary>
    Task<FluentValidation.Results.ValidationResult> ValidateCreateSubscriptionPaymentAsync(CreateSubscriptionPaymentRequest request);

    /// <summary>
    /// Validate create supplementary payment request
    /// </summary>
    Task<FluentValidation.Results.ValidationResult> ValidateCreateSupplementaryPaymentAsync(CreateSupplementaryPaymentRequest request);

    /// <summary>
    /// Validate update payment status request
    /// </summary>
    Task<FluentValidation.Results.ValidationResult> ValidateUpdatePaymentStatusAsync(UpdatePaymentStatusRequest request);

    /// <summary>
    /// Validate get payments paged request
    /// </summary>
    Task<FluentValidation.Results.ValidationResult> ValidateGetPaymentsPagedAsync(GetPaymentsPagedRequest request);

    /// <summary>
    /// Validate get payment statistics request
    /// </summary>
    Task<FluentValidation.Results.ValidationResult> ValidateGetPaymentStatisticsAsync(GetPaymentStatisticsRequest request);
}