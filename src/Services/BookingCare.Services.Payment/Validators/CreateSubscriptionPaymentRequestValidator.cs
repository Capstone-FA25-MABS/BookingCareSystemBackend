using FluentValidation;
using BookingCare.Services.Payment.Models.DTOs.Requests;

namespace BookingCare.Services.Payment.Validators;

/// <summary>
/// Validator cho CreateSubscriptionPaymentRequest
/// </summary>
public class CreateSubscriptionPaymentRequestValidator : AbstractValidator<CreateSubscriptionPaymentRequest>
{
    public CreateSubscriptionPaymentRequestValidator()
    {
        RuleFor(x => x.SubscriptionId)
            .NotEqual(Guid.Empty)
            .WithMessage("SubscriptionId không ???c ?? tr?ng");

        RuleFor(x => x.ClinicId)
            .NotEqual(Guid.Empty)
            .WithMessage("ClinicId không ???c ?? tr?ng");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount ph?i l?n h?n 0")
            .LessThanOrEqualTo(99999999.99m)
            .WithMessage("Amount không ???c v??t quá 99,999,999.99");

        RuleFor(x => x.PaymentMethodId)
            .NotEqual(Guid.Empty)
            .WithMessage("PaymentMethodId không ???c ?? tr?ng");
    }
}