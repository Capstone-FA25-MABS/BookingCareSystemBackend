using FluentValidation;
using BookingCare.Services.Payment.Models.DTOs.Requests;

namespace BookingCare.Services.Payment.Validators;

/// <summary>
/// Validator for CreateAppointmentPaymentRequest
/// </summary>
public class CreateAppointmentPaymentRequestValidator : AbstractValidator<CreateAppointmentPaymentRequest>
{
    public CreateAppointmentPaymentRequestValidator()
    {
        RuleFor(x => x.AppointmentId)
            .NotEqual(Guid.Empty)
            .WithMessage("AppointmentId must not be empty");

        RuleFor(x => x.PatientId)
            .NotEqual(Guid.Empty)
            .WithMessage("PatientId must not be empty");

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