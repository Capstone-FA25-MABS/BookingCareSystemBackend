using FluentValidation;
using BookingCare.Services.Communication.Models.DTOs;
using BookingCare.Services.Communication.Enums;

namespace BookingCare.Services.Communication.Validators;

/// <summary>
/// Validator cho CreateMessageRequest
/// </summary>
public class CreateMessageRequestValidator : AbstractValidator<CreateMessageRequest>
{
    public CreateMessageRequestValidator()
    {
        RuleFor(x => x.ConversationId)
            .NotEmpty()
            .WithMessage("ConversationId là b?t bu?c");

        RuleFor(x => x.SenderId)
            .NotEmpty()
            .WithMessage("SenderId là b?t bu?c");

        RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("Content là b?t bu?c")
            .MaximumLength(5000)
            .WithMessage("Content không ???c v??t quá 5000 ký t?");

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Type ph?i là m?t trong các giá tr? h?p l?: Text, Image, File, Video, Audio, System");

        // Validate attachments based on message type
        RuleFor(x => x)
            .Must(ValidateAttachmentsForType)
            .WithMessage("Attachments không phù h?p v?i lo?i tin nh?n");

        RuleForEach(x => x.Attachments)
            .SetValidator(new MessageAttachmentRequestValidator());
    }

    private bool ValidateAttachmentsForType(CreateMessageRequest request)
    {
        // Text and System messages shouldn't have attachments
        if ((request.Type == MessageType.Text || request.Type == MessageType.System) && request.Attachments.Any())
        {
            return false;
        }

        // Non-text messages should have attachments
        if (request.Type != MessageType.Text && request.Type != MessageType.System && !request.Attachments.Any())
        {
            return false;
        }

        return true;
    }
}

/// <summary>
/// Validator cho UpdateMessageRequest
/// </summary>
public class UpdateMessageRequestValidator : AbstractValidator<UpdateMessageRequest>
{
    public UpdateMessageRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Id là b?t bu?c");

        RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("Content là b?t bu?c")
            .MaximumLength(5000)
            .WithMessage("Content không ???c v??t quá 5000 ký t?");

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Type ph?i là m?t trong các giá tr? h?p l?: Text, Image, File, Video, Audio, System");

        RuleForEach(x => x.Attachments)
            .SetValidator(new MessageAttachmentRequestValidator());
    }
}

/// <summary>
/// Validator cho MessageAttachmentRequest
/// </summary>
public class MessageAttachmentRequestValidator : AbstractValidator<MessageAttachmentRequest>
{
    public MessageAttachmentRequestValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty()
            .WithMessage("Url là b?t bu?c")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Url ph?i là m?t URL h?p l?");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name là b?t bu?c")
            .MaximumLength(255)
            .WithMessage("Name không ???c v??t quá 255 ký t?");

        RuleFor(x => x.Size)
            .GreaterThan(0)
            .WithMessage("Size ph?i l?n h?n 0")
            .LessThanOrEqualTo(100 * 1024 * 1024) // 100MB
            .WithMessage("Size không ???c v??t quá 100MB");

        RuleFor(x => x.MimeType)
            .MaximumLength(100)
            .WithMessage("MimeType không ???c v??t quá 100 ký t?");
    }
}

/// <summary>
/// Validator cho MarkMessageAsReadRequest
/// </summary>
public class MarkMessageAsReadRequestValidator : AbstractValidator<MarkMessageAsReadRequest>
{
    public MarkMessageAsReadRequestValidator()
    {
        RuleFor(x => x.MessageId)
            .NotEmpty()
            .WithMessage("MessageId là b?t bu?c");
    }
}

/// <summary>
/// Validator cho SearchMessageRequest
/// </summary>
public class SearchMessageRequestValidator : AbstractValidator<SearchMessageRequest>
{
    public SearchMessageRequestValidator()
    {
        RuleFor(x => x.ConversationId)
            .NotEmpty()
            .WithMessage("ConversationId là b?t bu?c");

        RuleFor(x => x.SearchTerm)
            .NotEmpty()
            .WithMessage("SearchTerm là b?t bu?c")
            .MinimumLength(2)
            .WithMessage("SearchTerm ph?i có ít nh?t 2 ký t?")
            .MaximumLength(100)
            .WithMessage("SearchTerm không ???c v??t quá 100 ký t?");

        RuleFor(x => x.MessageType)
            .IsInEnum()
            .When(x => x.MessageType.HasValue)
            .WithMessage("MessageType ph?i là m?t trong các giá tr? h?p l?");

        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page ph?i l?n h?n 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("PageSize ph?i l?n h?n 0")
            .LessThanOrEqualTo(100)
            .WithMessage("PageSize không ???c v??t quá 100");
    }
}