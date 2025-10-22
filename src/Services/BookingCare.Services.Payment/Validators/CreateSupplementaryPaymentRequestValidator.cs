using FluentValidation;
using BookingCare.Services.Payment.Models.DTOs.Requests;

namespace BookingCare.Services.Payment.Validators;

/// <summary>
/// Validator for CreateSupplementaryPaymentRequest
/// </summary>
public class CreateSupplementaryPaymentRequestValidator : AbstractValidator<CreateSupplementaryPaymentRequest>
{
    public CreateSupplementaryPaymentRequestValidator()
    {
        RuleFor(x => x.AppointmentId)
            .NotEqual(Guid.Empty)
            .WithMessage("AppointmentId must not be empty");

        RuleFor(x => x.PatientId)
            .NotEqual(Guid.Empty)
            .WithMessage("PatientId must not be empty");

        RuleFor(x => x.AdditionalAmount)
            .GreaterThan(0)
            .WithMessage("AdditionalAmount must be greater than 0")
            .LessThanOrEqualTo(99999999.99m)
            .WithMessage("AdditionalAmount must not exceed 99,999,999.99");

        RuleFor(x => x.PaymentMethodId)
            .NotEqual(Guid.Empty)
            .WithMessage("PaymentMethodId must not be empty");

        RuleFor(x => x.RescheduleToken)
            .NotEmpty()
            .WithMessage("RescheduleToken is required")
            .MaximumLength(100)
            .WithMessage("RescheduleToken must not exceed 100 characters");

        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .WithMessage("Reason must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Reason));
    }
}
