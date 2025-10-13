using FluentValidation;
using BookingCare.Services.Payment.Models.DTOs.Requests;

namespace BookingCare.Services.Payment.Validators;

/// <summary>
/// Validator for CreatePaymentRequest
/// </summary>
public class CreatePaymentRequestValidator : AbstractValidator<CreatePaymentRequest>
{
    public CreatePaymentRequestValidator()
    {
        // AppointmentId can be null, but if present must not be Empty
        When(x => x.AppointmentId.HasValue, () =>
        {
            RuleFor(x => x.AppointmentId)
                .NotEqual(Guid.Empty)
                .WithMessage("AppointmentId must not be empty if provided");
        });

        // SubscriptionId can be null, but if present must not be Empty
        When(x => x.SubscriptionId.HasValue, () =>
        {
            RuleFor(x => x.SubscriptionId)
                .NotEqual(Guid.Empty)
                .WithMessage("SubscriptionId must not be empty if provided");
        });

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than 0")
            .LessThanOrEqualTo(99999999.99m)
            .WithMessage("Amount must not exceed 99,999,999.99");

        RuleFor(x => x.TransactionType)
            .IsInEnum()
            .WithMessage("TransactionType is not valid");

        RuleFor(x => x.PaymentMethodId)
            .NotEqual(Guid.Empty)
            .WithMessage("PaymentMethodId must not be empty");

        // ClinicId and PatientId can be null, validate if present
        When(x => x.ClinicId.HasValue, () =>
        {
            RuleFor(x => x.ClinicId)
                .NotEqual(Guid.Empty)
                .WithMessage("ClinicId must not be empty if provided");
        });

        When(x => x.PatientId.HasValue, () =>
        {
            RuleFor(x => x.PatientId)
                .NotEqual(Guid.Empty)
                .WithMessage("PatientId must not be empty if provided");
        });
    }
}