using FluentValidation;
using BookingCare.Services.Payment.Models.DTOs.Requests;

namespace BookingCare.Services.Payment.Validators;

/// <summary>
/// Validator for UpdatePaymentMethodStatusRequest
/// </summary>
public class UpdatePaymentMethodStatusRequestValidator : AbstractValidator<UpdatePaymentMethodStatusRequest>
{
    public UpdatePaymentMethodStatusRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty)
            .WithMessage("Id must not be empty");

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Status is not valid");
    }
}