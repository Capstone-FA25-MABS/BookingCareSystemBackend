using FluentValidation;
using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Services.Payment.Services.Interfaces;
using BookingCare.Shared.Common.Services;

namespace BookingCare.Services.Payment.Services.Implementations;

/// <summary>
/// Service implementation for payment-related validation operations
/// </summary>
public class PaymentValidationService : BaseService, IPaymentValidationService
{
    private readonly IValidator<CreateAppointmentPaymentRequest> _createAppointmentValidator;
    private readonly IValidator<CreateSubscriptionPaymentRequest> _createSubscriptionValidator;
    private readonly IValidator<CreateSupplementaryPaymentRequest> _createSupplementaryValidator;
    private readonly IValidator<UpdatePaymentStatusRequest> _updateValidator;
    private readonly IValidator<GetPaymentsPagedRequest> _pagedValidator;
    private readonly IValidator<GetPaymentStatisticsRequest> _statisticsValidator;

    public PaymentValidationService(
        IValidator<CreateAppointmentPaymentRequest> createAppointmentValidator,
        IValidator<CreateSubscriptionPaymentRequest> createSubscriptionValidator,
        IValidator<CreateSupplementaryPaymentRequest> createSupplementaryValidator,
        IValidator<UpdatePaymentStatusRequest> updateValidator,
        IValidator<GetPaymentsPagedRequest> pagedValidator,
        IValidator<GetPaymentStatisticsRequest> statisticsValidator,
        ILogger<PaymentValidationService> logger) : base(logger)
    {
        _createAppointmentValidator = createAppointmentValidator;
        _createSubscriptionValidator = createSubscriptionValidator;
        _createSupplementaryValidator = createSupplementaryValidator;
        _updateValidator = updateValidator;
        _pagedValidator = pagedValidator;
        _statisticsValidator = statisticsValidator;
    }

    /// <summary>
    /// Validate create appointment payment request
    /// </summary>
    public async Task<FluentValidation.Results.ValidationResult> ValidateCreateAppointmentPaymentAsync(CreateAppointmentPaymentRequest request)
    {
        return await _createAppointmentValidator.ValidateAsync(request);
    }

    /// <summary>
    /// Validate create subscription payment request
    /// </summary>
    public async Task<FluentValidation.Results.ValidationResult> ValidateCreateSubscriptionPaymentAsync(CreateSubscriptionPaymentRequest request)
    {
        return await _createSubscriptionValidator.ValidateAsync(request);
    }

    /// <summary>
    /// Validate create supplementary payment request
    /// </summary>
    public async Task<FluentValidation.Results.ValidationResult> ValidateCreateSupplementaryPaymentAsync(CreateSupplementaryPaymentRequest request)
    {
        return await _createSupplementaryValidator.ValidateAsync(request);
    }

    /// <summary>
    /// Validate update payment status request
    /// </summary>
    public async Task<FluentValidation.Results.ValidationResult> ValidateUpdatePaymentStatusAsync(UpdatePaymentStatusRequest request)
    {
        return await _updateValidator.ValidateAsync(request);
    }

    /// <summary>
    /// Validate get payments paged request
    /// </summary>
    public async Task<FluentValidation.Results.ValidationResult> ValidateGetPaymentsPagedAsync(GetPaymentsPagedRequest request)
    {
        return await _pagedValidator.ValidateAsync(request);
    }

    /// <summary>
    /// Validate get payment statistics request
    /// </summary>
    public async Task<FluentValidation.Results.ValidationResult> ValidateGetPaymentStatisticsAsync(GetPaymentStatisticsRequest request)
    {
        return await _statisticsValidator.ValidateAsync(request);
    }
}