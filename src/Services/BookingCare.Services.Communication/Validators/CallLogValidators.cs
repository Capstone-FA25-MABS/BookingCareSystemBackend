using FluentValidation;
using BookingCare.Services.Communication.Models.DTOs;
using BookingCare.Services.Communication.Enums;

namespace BookingCare.Services.Communication.Validators;

/// <summary>
/// Validator cho CreateCallLogRequest
/// </summary>
public class CreateCallLogRequestValidator : AbstractValidator<CreateCallLogRequest>
{
    public CreateCallLogRequestValidator()
    {
        RuleFor(x => x.ConversationId)
            .NotEmpty()
            .WithMessage("ConversationId là b?t bu?c");

        RuleFor(x => x.CallerId)
            .NotEmpty()
            .WithMessage("CallerId là b?t bu?c");

        RuleFor(x => x.ReceiverId)
            .NotEmpty()
            .WithMessage("ReceiverId là b?t bu?c");

        RuleFor(x => x.CallerId)
            .NotEqual(x => x.ReceiverId)
            .WithMessage("CallerId và ReceiverId không ???c gi?ng nhau");

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Type ph?i là Audio ho?c Video");
    }
}

/// <summary>
/// Validator cho UpdateCallLogRequest
/// </summary>
public class UpdateCallLogRequestValidator : AbstractValidator<UpdateCallLogRequest>
{
    public UpdateCallLogRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Id là b?t bu?c");

        RuleFor(x => x.Duration)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Duration ph?i l?n h?n ho?c b?ng 0")
            .LessThanOrEqualTo(24 * 60) // 24 gi?
            .WithMessage("Duration không ???c v??t quá 24 gi?");

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Status ph?i là Accepted, Missed, ho?c Rejected");

        RuleFor(x => x.EndedAt)
            .Must((request, endedAt) =>
                !endedAt.HasValue ||
                endedAt.Value >= DateTime.UtcNow.AddDays(-1))
            .WithMessage("EndedAt không ???c là th?i gian quá xa trong quá kh?");
    }
}

/// <summary>
/// Validator cho GetCallStatisticsRequest
/// </summary>
public class GetCallStatisticsRequestValidator : AbstractValidator<GetCallStatisticsRequest>
{
    public GetCallStatisticsRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId là b?t bu?c");

        RuleFor(x => x.FromDate)
            .LessThanOrEqualTo(x => x.ToDate)
            .WithMessage("FromDate ph?i nh? h?n ho?c b?ng ToDate");

        RuleFor(x => x.ToDate)
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .WithMessage("ToDate không ???c là th?i gian trong t??ng lai");

        RuleFor(x => x.FromDate)
            .GreaterThanOrEqualTo(DateTime.UtcNow.AddYears(-2))
            .WithMessage("FromDate không ???c quá 2 n?m trong quá kh?");
    }
}