using FluentValidation;
using BookingCare.Services.Payment.Models.DTOs.Requests;

namespace BookingCare.Services.Payment.Validators;

/// <summary>
/// Validator cho CreatePaymentRequest
/// </summary>
public class CreatePaymentRequestValidator : AbstractValidator<CreatePaymentRequest>
{
    public CreatePaymentRequestValidator()
    {
        // AppointmentId có th? null, nh?ng n?u có thì không ???c Empty
        When(x => x.AppointmentId.HasValue, () =>
        {
            RuleFor(x => x.AppointmentId)
                .NotEqual(Guid.Empty)
                .WithMessage("AppointmentId không ???c ?? tr?ng n?u ???c cung c?p");
        });

        // SubscriptionId có th? null, nh?ng n?u có thì không ???c Empty
        When(x => x.SubscriptionId.HasValue, () =>
        {
            RuleFor(x => x.SubscriptionId)
                .NotEqual(Guid.Empty)
                .WithMessage("SubscriptionId không ???c ?? tr?ng n?u ???c cung c?p");
        });

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount ph?i l?n h?n 0")
            .LessThanOrEqualTo(99999999.99m)
            .WithMessage("Amount không ???c v??t quá 99,999,999.99");

        RuleFor(x => x.TransactionType)
            .IsInEnum()
            .WithMessage("TransactionType không h?p l?");

        RuleFor(x => x.PaymentMethodId)
            .NotEqual(Guid.Empty)
            .WithMessage("PaymentMethodId không ???c ?? tr?ng");

        // ClinicId và PatientId có th? null, validate n?u có giá tr?
        When(x => x.ClinicId.HasValue, () =>
        {
            RuleFor(x => x.ClinicId)
                .NotEqual(Guid.Empty)
                .WithMessage("ClinicId không ???c ?? tr?ng n?u ???c cung c?p");
        });

        When(x => x.PatientId.HasValue, () =>
        {
            RuleFor(x => x.PatientId)
                .NotEqual(Guid.Empty)
                .WithMessage("PatientId không ???c ?? tr?ng n?u ???c cung c?p");
        });
    }
}