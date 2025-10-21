using FluentValidation;
using BookingCare.Services.Communication.Models.DTOs;
using BookingCare.Services.Communication.Enums;

namespace BookingCare.Services.Communication.Validators;

/// <summary>
/// Validator for CreateMessageRequest
/// </summary>
public class CreateMessageRequestValidator : AbstractValidator<CreateMessageRequest>
{
    public CreateMessageRequestValidator()
    {
        RuleFor(x => x.ConversationId)
            .NotEmpty()
            .WithMessage("ConversationId is required");

        RuleFor(x => x.SenderId)
            .NotEmpty()
            .WithMessage("SenderId is required");

        RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("Content is required")
            .MaximumLength(5000)
            .WithMessage("Content must not exceed 5000 characters");

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Type must be one of the valid values: Text, Image, File, Video, Audio, System");

        // Validate attachments based on message type
        RuleFor(x => x)
            .Must(ValidateAttachmentsForType)
            .WithMessage("Attachments are not valid for the specified message type");

        RuleForEach(x => x.Attachments)
            .SetValidator(new MessageAttachmentRequestValidator());
    }

    private static bool ValidateAttachmentsForType(CreateMessageRequest request)
    {
        // Text and System messages shouldn't have attachments
        if ((request.Type == MessageType.Text || request.Type == MessageType.System) && request.Attachments.Any())
        {
            return false;
        }

        // Non-text/system messages should have at least one attachment
        if (request.Type != MessageType.Text && request.Type != MessageType.System && !request.Attachments.Any())
        {
            return false;
        }

        return true;
    }
}

/// <summary>
/// Validator for UpdateMessageRequest
/// </summary>
public class UpdateMessageRequestValidator : AbstractValidator<UpdateMessageRequest>
{
    public UpdateMessageRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Id is required");

        RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("Content is required")
            .MaximumLength(5000)
            .WithMessage("Content must not exceed 5000 characters");

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Type must be one of the valid values: Text, Image, File, Video, Audio, System");

        RuleForEach(x => x.Attachments)
            .SetValidator(new MessageAttachmentRequestValidator());
    }
}

/// <summary>
/// Validator for MessageAttachmentRequest
/// </summary>
public class MessageAttachmentRequestValidator : AbstractValidator<MessageAttachmentRequest>
{
    public MessageAttachmentRequestValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty()
            .WithMessage("Url is required")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Url must be a valid absolute URL");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MaximumLength(255)
            .WithMessage("Name must not exceed 255 characters");

        RuleFor(x => x.Size)
            .GreaterThan(0)
            .WithMessage("Size must be greater than 0")
            .LessThanOrEqualTo(100 * 1024 * 1024) // 100MB
            .WithMessage("Size must not exceed 100MB");

        RuleFor(x => x.MimeType)
            .MaximumLength(100)
            .WithMessage("MimeType must not exceed 100 characters");
    }
}

/// <summary>
/// Validator for MarkMessageAsReadRequest
/// </summary>
public class MarkMessageAsReadRequestValidator : AbstractValidator<MarkMessageAsReadRequest>
{
    public MarkMessageAsReadRequestValidator()
    {
        RuleFor(x => x.MessageId)
            .NotEmpty()
            .WithMessage("MessageId is required");
    }
}

/// <summary>
/// Validator for SearchMessageRequest
/// </summary>
public class SearchMessageRequestValidator : AbstractValidator<SearchMessageRequest>
{
    public SearchMessageRequestValidator()
    {
        RuleFor(x => x.ConversationId)
            .NotEmpty()
            .WithMessage("ConversationId is required");

        RuleFor(x => x.SearchTerm)
            .NotEmpty()
            .WithMessage("SearchTerm is required")
            .MinimumLength(2)
            .WithMessage("SearchTerm must be at least 2 characters")
            .MaximumLength(100)
            .WithMessage("SearchTerm must not exceed 100 characters");

        RuleFor(x => x.MessageType)
            .IsInEnum()
            .When(x => x.MessageType.HasValue)
            .WithMessage("MessageType must be a valid value");

        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("PageSize must be greater than 0")
            .LessThanOrEqualTo(100)
            .WithMessage("PageSize must not exceed 100");
    }
}