using FluentValidation;
using BookingCare.Services.Communication.Models.DTOs;
using BookingCare.Services.Communication.Enums;

namespace BookingCare.Services.Communication.Validators;

/// <summary>
/// Validator for CreateCallLogRequest
/// </summary>
public class CreateCallLogRequestValidator : AbstractValidator<CreateCallLogRequest>
{
    public CreateCallLogRequestValidator()
    {
        RuleFor(x => x.ConversationId)
            .NotEmpty()
            .WithMessage("ConversationId is required");

        RuleFor(x => x.CallerId)
            .NotEmpty()
            .WithMessage("CallerId is required");

        RuleFor(x => x.ReceiverId)
            .NotEmpty()
            .WithMessage("ReceiverId is required");

        RuleFor(x => x.CallerId)
            .NotEqual(x => x.ReceiverId)
            .WithMessage("CallerId and ReceiverId must not be the same");

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Type must be Audio or Video");
    }
}

/// <summary>
/// Validator for UpdateCallLogRequest
/// </summary>
public class UpdateCallLogRequestValidator : AbstractValidator<UpdateCallLogRequest>
{
    public UpdateCallLogRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Id is required");

        RuleFor(x => x.Duration)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Duration must be greater than or equal to 0")
            .LessThanOrEqualTo(24 * 60) // 24 hours
            .WithMessage("Duration must not exceed 24 hours");

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Status must be Accepted, Missed, or Rejected");

        RuleFor(x => x.EndedAt)
            .Must((request, endedAt) =>
                !endedAt.HasValue ||
                endedAt.Value >= DateTime.UtcNow.AddDays(-1))
            .WithMessage("EndedAt must not be a time too far in the past");
    }
}

/// <summary>
/// Validator for GetCallStatisticsRequest
/// </summary>
public class GetCallStatisticsRequestValidator : AbstractValidator<GetCallStatisticsRequest>
{
    public GetCallStatisticsRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required");

        RuleFor(x => x.FromDate)
            .LessThanOrEqualTo(x => x.ToDate)
            .WithMessage("FromDate must be less than or equal to ToDate");

        RuleFor(x => x.ToDate)
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .WithMessage("ToDate must not be a future time");

        RuleFor(x => x.FromDate)
            .GreaterThanOrEqualTo(DateTime.UtcNow.AddYears(-2))
            .WithMessage("FromDate must not be more than 2 years in the past");
    }
}