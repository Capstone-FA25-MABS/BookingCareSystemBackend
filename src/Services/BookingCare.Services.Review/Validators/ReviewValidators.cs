using BookingCare.Services.Review.Enums;
using BookingCare.Services.Review.Extensions;
using BookingCare.Services.Review.Models.DTOs;
using FluentValidation;

namespace BookingCare.Services.Review.Validators;

/// <summary>
/// Enhanced validator for CreateReviewRequest with detailed error messages
/// </summary>
public class CreateReviewRequestValidator : AbstractValidator<CreateReviewRequest>
{
    public CreateReviewRequestValidator()
    {
        RuleFor(x => x.PatientId).NotEmptyGuid().WithName("Patient ID");

        RuleFor(x => x.TargetType)
            .IsInEnum()
            .WithMessage("Target type must be either 'DOCTOR' or 'SERVICE'");

        // Conditional validation for Doctor reviews
        RuleFor(x => x.DoctorId)
            .NotEmptyGuidRequired()
            .When(x => x.TargetType == TargetType.DOCTOR)
            .WithMessage("Doctor ID is required when reviewing a doctor")
            .WithName("Doctor ID");

        RuleFor(x => x.ServiceId)
            .NotEmptyGuidRequired()
            .When(x => x.TargetType == TargetType.SERVICE)
            .WithMessage("Service ID is required when reviewing a service")
            .WithName("Service ID");

        // Ensure mutual exclusivity
        RuleFor(x => x.DoctorId)
            .Empty()
            .When(x => x.TargetType == TargetType.SERVICE)
            .WithMessage("Doctor ID should not be provided when reviewing a service");

        RuleFor(x => x.ServiceId)
            .Empty()
            .When(x => x.TargetType == TargetType.DOCTOR)
            .WithMessage("Service ID should not be provided when reviewing a doctor");

        RuleFor(x => x.Rating).ValidRating();

        RuleFor(x => x.Comment).ValidComment();

        // Cross-field validation
        RuleFor(x => x)
            .Must(BeValidTargetCombination)
            .WithMessage(
                "Invalid target combination: Either Doctor ID or Service ID must be provided based on target type"
            )
            .OverridePropertyName("TargetCombination");
    }

    private static bool BeValidTargetCombination(CreateReviewRequest request)
    {
        return request.TargetType switch
        {
            TargetType.DOCTOR => request.DoctorId.HasValue
                && request.DoctorId != Guid.Empty
                && !request.ServiceId.HasValue,
            TargetType.SERVICE => request.ServiceId.HasValue
                && request.ServiceId != Guid.Empty
                && !request.DoctorId.HasValue,
            _ => false,
        };
    }
}

/// <summary>
/// Enhanced validator for UpdateReviewRequest
/// </summary>
public class UpdateReviewRequestValidator : AbstractValidator<UpdateReviewRequest>
{
    public UpdateReviewRequestValidator()
    {
        RuleFor(x => x.Id).ValidObjectId().WithName("Review ID");

        RuleFor(x => x.Rating).ValidRating();

        RuleFor(x => x.Comment).ValidComment();
    }
}

/// <summary>
/// Validator for GetReviewsRequest
/// </summary>
public class GetReviewsRequestValidator : AbstractValidator<GetReviewsRequest>
{
    public GetReviewsRequestValidator()
    {
        When(
            x => x.PatientId.HasValue,
            () =>
            {
                RuleFor(x => x.PatientId!.Value)
                    .NotEmpty()
                    .WithMessage("Patient ID cannot be empty")
                    .WithName("Patient ID");
            }
        );

        When(
            x => x.DoctorId.HasValue,
            () =>
            {
                RuleFor(x => x.DoctorId!.Value)
                    .NotEmpty()
                    .WithMessage("Doctor ID cannot be empty")
                    .WithName("Doctor ID");
            }
        );

        When(
            x => x.ServiceId.HasValue,
            () =>
            {
                RuleFor(x => x.ServiceId!.Value)
                    .NotEmpty()
                    .WithMessage("Service ID cannot be empty")
                    .WithName("Service ID");
            }
        );

        RuleFor(x => x.TargetType)
            .IsInEnum()
            .When(x => x.TargetType.HasValue)
            .WithMessage("Target type must be either 'DOCTOR' or 'SERVICE'");

        When(
            x => x.MinRating.HasValue,
            () =>
            {
                RuleFor(x => x.MinRating!.Value).ValidRating();
            }
        );

        When(
            x => x.MaxRating.HasValue,
            () =>
            {
                RuleFor(x => x.MaxRating!.Value).ValidRating();
            }
        );

        RuleFor(x => x)
            .Must(BeValidRatingRange)
            .WithMessage("Maximum rating must be greater than or equal to minimum rating")
            .When(x => x.MinRating.HasValue && x.MaxRating.HasValue)
            .OverridePropertyName("RatingRange");

        RuleFor(x => x.Page).ValidPage();

        RuleFor(x => x.PageSize).ValidPageSize();
    }

    private static bool BeValidRatingRange(GetReviewsRequest request)
    {
        if (request.MinRating.HasValue && request.MaxRating.HasValue)
        {
            return request.MaxRating >= request.MinRating;
        }
        return true;
    }
}

/// <summary>
/// Enhanced validator for AddReplyRequest
/// </summary>
public class AddReplyRequestValidator : AbstractValidator<AddReplyRequest>
{
    public AddReplyRequestValidator()
    {
        RuleFor(x => x.ReviewId).ValidObjectId().WithName("Review ID");

        RuleFor(x => x.AuthorId).NotEmptyGuid().WithName("Author ID");

        RuleFor(x => x.Content).ValidReplyContent();
    }
}

/// <summary>
/// Enhanced validator for UpdateReplyRequest
/// </summary>
public class UpdateReplyRequestValidator : AbstractValidator<UpdateReplyRequest>
{
    public UpdateReplyRequestValidator()
    {
        RuleFor(x => x.ReviewId).ValidObjectId().WithName("Review ID");

        RuleFor(x => x.ReplyId).ValidObjectId().WithName("Reply ID");

        RuleFor(x => x.Content).ValidReplyContent();
    }
}

/// <summary>
/// Validator for BatchDoctorsStatisticsRequest
/// </summary>
public class BatchDoctorsStatisticsRequestValidator
    : AbstractValidator<BatchDoctorsStatisticsRequest>
{
    public BatchDoctorsStatisticsRequestValidator()
    {
        RuleFor(x => x.DoctorIds)
            .NotEmpty()
            .WithMessage("At least one doctor ID must be provided")
            .Must(x => x.Count <= 500)
            .WithMessage("Maximum 500 doctor IDs allowed per request");

        RuleForEach(x => x.DoctorIds).NotEmptyGuid().WithName("Doctor ID");

        When(
            x => x.HospitalId.HasValue,
            () =>
            {
                RuleFor(x => x.HospitalId!.Value)
                    .NotEmpty()
                    .WithMessage("Hospital ID cannot be empty")
                    .WithName("Hospital ID");
            }
        );
    }
}

/// <summary>
/// Validator for BatchServicesStatisticsRequest
/// </summary>
public class BatchServicesStatisticsRequestValidator
    : AbstractValidator<BatchServicesStatisticsRequest>
{
    public BatchServicesStatisticsRequestValidator()
    {
        RuleFor(x => x.ServiceIds)
            .NotEmpty()
            .WithMessage("At least one service ID must be provided")
            .Must(x => x.Count <= 500)
            .WithMessage("Maximum 500 service IDs allowed per request");

        RuleForEach(x => x.ServiceIds).NotEmptyGuid().WithName("Service ID");

        When(
            x => x.HospitalId.HasValue,
            () =>
            {
                RuleFor(x => x.HospitalId!.Value)
                    .NotEmpty()
                    .WithMessage("Hospital ID cannot be empty")
                    .WithName("Hospital ID");
            }
        );
    }
}
