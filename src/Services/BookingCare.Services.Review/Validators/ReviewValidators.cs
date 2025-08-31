using FluentValidation;
using BookingCare.Services.Review.Models.DTOs;
using BookingCare.Services.Review.Models.Enums;

namespace BookingCare.Services.Review.Validators;

/// <summary>
/// Validator for CreateReviewRequest
/// </summary>
public class CreateReviewRequestValidator : AbstractValidator<CreateReviewRequest>
{
    public CreateReviewRequestValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty()
            .WithMessage("Patient ID is required");

        RuleFor(x => x.TargetType)
            .IsInEnum()
            .WithMessage("Target type must be DOCTOR or SERVICE");

        RuleFor(x => x.DoctorId)
            .NotEmpty()
            .When(x => x.TargetType == TargetType.DOCTOR)
            .WithMessage("Doctor ID is required when reviewing a doctor");

        RuleFor(x => x.ClinicServiceId)
            .NotEmpty()
            .When(x => x.TargetType == TargetType.SERVICE)
            .WithMessage("Clinic Service ID is required when reviewing a service");

        RuleFor(x => x.DoctorId)
            .Empty()
            .When(x => x.TargetType == TargetType.SERVICE)
            .WithMessage("Doctor ID should be null when reviewing a service");

        RuleFor(x => x.ClinicServiceId)
            .Empty()
            .When(x => x.TargetType == TargetType.DOCTOR)
            .WithMessage("Clinic Service ID should be null when reviewing a doctor");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5)
            .WithMessage("Rating must be between 1 and 5 stars");

        RuleFor(x => x.Comment)
            .NotEmpty()
            .WithMessage("Comment is required")
            .MaximumLength(1000)
            .WithMessage("Comment cannot exceed 1000 characters");
    }
}

/// <summary>
/// Validator for UpdateReviewRequest
/// </summary>
public class UpdateReviewRequestValidator : AbstractValidator<UpdateReviewRequest>
{
    public UpdateReviewRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Review ID is required");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5)
            .WithMessage("Rating must be between 1 and 5 stars");

        RuleFor(x => x.Comment)
            .NotEmpty()
            .WithMessage("Comment is required")
            .MaximumLength(1000)
            .WithMessage("Comment cannot exceed 1000 characters");
    }
}

/// <summary>
/// Validator for AddReplyRequest
/// </summary>
public class AddReplyRequestValidator : AbstractValidator<AddReplyRequest>
{
    public AddReplyRequestValidator()
    {
        RuleFor(x => x.ReviewId)
            .NotEmpty()
            .WithMessage("Review ID is required");

        RuleFor(x => x.AuthorId)
            .NotEmpty()
            .WithMessage("Author ID is required");

        RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("Reply content is required")
            .MaximumLength(500)
            .WithMessage("Reply content cannot exceed 500 characters");
    }
}

/// <summary>
/// Validator for UpdateReplyRequest
/// </summary>
public class UpdateReplyRequestValidator : AbstractValidator<UpdateReplyRequest>
{
    public UpdateReplyRequestValidator()
    {
        RuleFor(x => x.ReviewId)
            .NotEmpty()
            .WithMessage("Review ID is required");

        RuleFor(x => x.ReplyId)
            .NotEmpty()
            .WithMessage("Reply ID is required");

        RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("Reply content is required")
            .MaximumLength(500)
            .WithMessage("Reply content cannot exceed 500 characters");
    }
}

/// <summary>
/// Validator for GetReviewsRequest
/// </summary>
public class GetReviewsRequestValidator : AbstractValidator<GetReviewsRequest>
{
    public GetReviewsRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100");

        RuleFor(x => x.MinRating)
            .InclusiveBetween(1, 5)
            .When(x => x.MinRating.HasValue)
            .WithMessage("Minimum rating must be between 1 and 5");

        RuleFor(x => x.MaxRating)
            .InclusiveBetween(1, 5)
            .When(x => x.MaxRating.HasValue)
            .WithMessage("Maximum rating must be between 1 and 5");

        RuleFor(x => x.MaxRating)
            .GreaterThanOrEqualTo(x => x.MinRating)
            .When(x => x.MinRating.HasValue && x.MaxRating.HasValue)
            .WithMessage("Maximum rating must be greater than or equal to minimum rating");

        RuleFor(x => x.TargetType)
            .IsInEnum()
            .When(x => x.TargetType.HasValue)
            .WithMessage("Target type must be DOCTOR or SERVICE");
    }
}