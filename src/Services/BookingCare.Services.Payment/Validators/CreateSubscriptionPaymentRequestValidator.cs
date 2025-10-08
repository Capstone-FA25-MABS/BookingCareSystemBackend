using FluentValidation;
using BookingCare.Services.Payment.Models.DTOs.Requests;

namespace BookingCare.Services.Payment.Validators;

/// <summary>
/// Validator for CreateSubscriptionPaymentRequest
/// </summary>
public class CreateSubscriptionPaymentRequestValidator : AbstractValidator<CreateSubscriptionPaymentRequest>
{
    public CreateSubscriptionPaymentRequestValidator()
    {
        RuleFor(x => x.SubscriptionId)
            .NotEqual(Guid.Empty)
            .WithMessage("SubscriptionId must not be empty");

        RuleFor(x => x.ClinicId)
            .NotEqual(Guid.Empty)
            .WithMessage("ClinicId must not be empty");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than 0")
            .LessThanOrEqualTo(99999999.99m)
            .WithMessage("Amount must not exceed 99,999,999.99");

        RuleFor(x => x.PaymentMethodId)
            .NotEqual(Guid.Empty)
            .WithMessage("PaymentMethodId must not be empty");
    }
}