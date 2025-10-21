using FluentValidation;
using BookingCare.Services.Communication.Models.DTOs;

namespace BookingCare.Services.Communication.Validators;

/// <summary>
/// Validator for CreateConversationRequest
/// </summary>
public class CreateConversationRequestValidator : AbstractValidator<CreateConversationRequest>
{
    public CreateConversationRequestValidator()
    {
        RuleFor(x => x.Participants)
            .NotNull()
            .WithMessage("Participants are required")
            .Must(participants => participants.Count >= 2)
            .WithMessage("A conversation must have at least 2 participants")
            .Must(participants => participants.Count <= 10)
            .WithMessage("A conversation must not have more than 10 participants")
            .Must(participants => participants.Distinct().Count() == participants.Count)
            .WithMessage("Participants must be unique");

        RuleForEach(x => x.Participants)
            .NotEmpty()
            .WithMessage("Participant ID must not be empty");
    }
}

/// <summary>
/// Validator for BlockConversationRequest
/// </summary>
public class BlockConversationRequestValidator : AbstractValidator<BlockConversationRequest>
{
    public BlockConversationRequestValidator()
    {
        RuleFor(x => x.ConversationId)
            .NotEmpty()
            .WithMessage("ConversationId is required");

        RuleFor(x => x.BlockedBy)
            .NotEmpty()
            .WithMessage("BlockedBy is required");
    }
}

/// <summary>
/// Validator for UnblockConversationRequest
/// </summary>
public class UnblockConversationRequestValidator : AbstractValidator<UnblockConversationRequest>
{
    public UnblockConversationRequestValidator()
    {
        RuleFor(x => x.ConversationId)
            .NotEmpty()
            .WithMessage("ConversationId is required");
    }
}