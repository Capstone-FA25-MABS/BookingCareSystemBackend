using FluentValidation;
using BookingCare.Services.Communication.Models.DTOs;

namespace BookingCare.Services.Communication.Validators;

/// <summary>
/// Validator cho CreateConversationRequest
/// </summary>
public class CreateConversationRequestValidator : AbstractValidator<CreateConversationRequest>
{
    public CreateConversationRequestValidator()
    {
        RuleFor(x => x.Participants)
            .NotNull()
            .WithMessage("Participants là b?t bu?c")
            .Must(participants => participants.Count >= 2)
            .WithMessage("Cu?c h?i tho?i ph?i có ít nh?t 2 thành viên")
            .Must(participants => participants.Count <= 10)
            .WithMessage("Cu?c h?i tho?i không ???c có quá 10 thành viên")
            .Must(participants => participants.Distinct().Count() == participants.Count)
            .WithMessage("Không ???c có thành viên trùng l?p");

        RuleForEach(x => x.Participants)
            .NotEmpty()
            .WithMessage("ID thành viên không ???c ?? tr?ng");
    }
}

/// <summary>
/// Validator cho BlockConversationRequest
/// </summary>
public class BlockConversationRequestValidator : AbstractValidator<BlockConversationRequest>
{
    public BlockConversationRequestValidator()
    {
        RuleFor(x => x.ConversationId)
            .NotEmpty()
            .WithMessage("ConversationId là b?t bu?c");

        RuleFor(x => x.BlockedBy)
            .NotEmpty()
            .WithMessage("BlockedBy là b?t bu?c");
    }
}

/// <summary>
/// Validator cho UnblockConversationRequest
/// </summary>
public class UnblockConversationRequestValidator : AbstractValidator<UnblockConversationRequest>
{
    public UnblockConversationRequestValidator()
    {
        RuleFor(x => x.ConversationId)
            .NotEmpty()
            .WithMessage("ConversationId là b?t bu?c");
    }
}