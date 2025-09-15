using FluentValidation;
using BookingCare.Services.Payment.Models.DTOs.Requests;

namespace BookingCare.Services.Payment.Validators;

/// <summary>
/// Validator cho UpdatePaymentStatusRequest
/// </summary>
public class UpdatePaymentStatusRequestValidator : AbstractValidator<UpdatePaymentStatusRequest>
{
    public UpdatePaymentStatusRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty)
            .WithMessage("Id không ???c ?? tr?ng");

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Status không h?p l?");
    }
}