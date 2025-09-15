using FluentValidation;
using BookingCare.Services.Payment.Models.DTOs.Requests;

namespace BookingCare.Services.Payment.Validators;

/// <summary>
/// Validator cho UpdatePaymentMethodStatusRequest
/// </summary>
public class UpdatePaymentMethodStatusRequestValidator : AbstractValidator<UpdatePaymentMethodStatusRequest>
{
    public UpdatePaymentMethodStatusRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty)
            .WithMessage("ID kh?ng ???c ?? tr?ng");

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Tr?ng thái kh?ng h?p l?");
    }
}