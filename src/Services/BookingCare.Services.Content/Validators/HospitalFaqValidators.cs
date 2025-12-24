using BookingCare.Services.Content.Models.DTOs;
using FluentValidation;

namespace BookingCare.Services.Content.Validators;

public class CreateHospitalFaqRequestValidator : AbstractValidator<CreateHospitalFaqRequest>
{
    public CreateHospitalFaqRequestValidator()
    {
        RuleFor(x => x.HospitalId)
            .NotEmpty().WithMessage("Hospital ID is required");

        RuleFor(x => x.Question)
            .NotEmpty().WithMessage("Question is required")
            .MaximumLength(1000).WithMessage("Question must not exceed 1000 characters");

        RuleFor(x => x.Answer)
            .NotEmpty().WithMessage("Answer is required")
            .MaximumLength(5000).WithMessage("Answer must not exceed 5000 characters");

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Display order must be greater than or equal to 0");
    }
}

public class UpdateHospitalFaqRequestValidator : AbstractValidator<UpdateHospitalFaqRequest>
{
    public UpdateHospitalFaqRequestValidator()
    {
        RuleFor(x => x.Question)
            .NotEmpty().WithMessage("Question is required")
            .MaximumLength(1000).WithMessage("Question must not exceed 1000 characters");

        RuleFor(x => x.Answer)
            .NotEmpty().WithMessage("Answer is required")
            .MaximumLength(5000).WithMessage("Answer must not exceed 5000 characters");

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Display order must be greater than or equal to 0");
    }
}


