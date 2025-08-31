using FluentValidation;
using BookingCare.Services.Review.Models.DTOs;
using BookingCare.Services.Review.Models.Enums;
using BookingCare.Services.Review.Extensions;

namespace BookingCare.Services.Review.Validators;

/// <summary>
/// Enhanced validator for CreateReviewRequest with detailed error messages
/// </summary>
public class CreateReviewRequestValidator : AbstractValidator<CreateReviewRequest>
{
    public CreateReviewRequestValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmptyGuid()
            .WithName("Patient ID");

        RuleFor(x => x.TargetType)
            .IsInEnum()
            .WithMessage("Target type must be either 'DOCTOR' or 'SERVICE'");

        // Conditional validation for Doctor reviews
        RuleFor(x => x.DoctorId)
            .NotEmptyGuidRequired()
            .When(x => x.TargetType == TargetType.DOCTOR)
            .WithMessage("Doctor ID is required when reviewing a doctor")
            .WithName("Doctor ID");

        RuleFor(x => x.ClinicServiceId)
            .NotEmptyGuidRequired()
            .When(x => x.TargetType == TargetType.SERVICE)
            .WithMessage("Clinic Service ID is required when reviewing a service")
            .WithName("Clinic Service ID");

        // Ensure mutual exclusivity
        RuleFor(x => x.DoctorId)
            .Empty()
            .When(x => x.TargetType == TargetType.SERVICE)
            .WithMessage("Doctor ID should not be provided when reviewing a service");

        RuleFor(x => x.ClinicServiceId)
            .Empty()
            .When(x => x.TargetType == TargetType.DOCTOR)
            .WithMessage("Clinic Service ID should not be provided when reviewing a doctor");

        RuleFor(x => x.Rating)
            .ValidRating();

        RuleFor(x => x.Comment)
            .ValidComment();

        // Cross-field validation
        RuleFor(x => x)
            .Must(BeValidTargetCombination)
            .WithMessage("Invalid target combination: Either Doctor ID or Clinic Service ID must be provided based on target type")
            .OverridePropertyName("TargetCombination");
    }

    private static bool BeValidTargetCombination(CreateReviewRequest request)
    {
        return request.TargetType switch
        {
            TargetType.DOCTOR => request.DoctorId.HasValue && request.DoctorId != Guid.Empty && !request.ClinicServiceId.HasValue,
            TargetType.SERVICE => request.ClinicServiceId.HasValue && request.ClinicServiceId != Guid.Empty && !request.DoctorId.HasValue,
            _ => false
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
        RuleFor(x => x.Id)
            .ValidObjectId()
            .WithName("Review ID");

        RuleFor(x => x.Rating)
            .ValidRating();

        RuleFor(x => x.Comment)
            .ValidComment();
    }
}

/// <summary>
/// Enhanced validator for AddReplyRequest
/// </summary>
public class AddReplyRequestValidator : AbstractValidator<AddReplyRequest>
{
    public AddReplyRequestValidator()
    {
        RuleFor(x => x.ReviewId)
            .ValidObjectId()
            .WithName("Review ID");

        RuleFor(x => x.AuthorId)
            .NotEmptyGuid()
            .WithName("Author ID");

        RuleFor(x => x.Content)
            .ValidReplyContent();
    }
}

/// <summary>
/// Enhanced validator for UpdateReplyRequest
/// </summary>
public class UpdateReplyRequestValidator : AbstractValidator<UpdateReplyRequest>
{
    public UpdateReplyRequestValidator()
    {
        RuleFor(x => x.ReviewId)
            .ValidObjectId()
            .WithName("Review ID");

        RuleFor(x => x.ReplyId)
            .ValidObjectId()
            .WithName("Reply ID");

        RuleFor(x => x.Content)
            .ValidReplyContent();
    }
}

/// <summary>
/// Enhanced validator for BatchDoctorsStatisticsRequest
/// </summary>
public class BatchDoctorsStatisticsRequestValidator : AbstractValidator<BatchDoctorsStatisticsRequest>
{
    public BatchDoctorsStatisticsRequestValidator()
    {
        RuleFor(x => x.DoctorIds)
            .NotEmpty()
            .WithMessage("Doctor IDs list cannot be empty");

        RuleFor(x => x.DoctorIds.Count)
            .ValidBatchSize(100)
            .WithName("Batch Size");

        RuleForEach(x => x.DoctorIds)
            .NotEmptyGuid()
            .WithMessage("All Doctor IDs must be valid non-empty GUIDs");

        // Check for duplicates
        RuleFor(x => x.DoctorIds)
            .Must(BeUnique)
            .WithMessage("Doctor IDs list should not contain duplicates");
    }

    private static bool BeUnique(List<Guid> doctorIds)
    {
        return doctorIds.Count == doctorIds.Distinct().Count();
    }
}

/// <summary>
/// Enhanced validator for BatchServicesStatisticsRequest
/// </summary>
public class BatchServicesStatisticsRequestValidator : AbstractValidator<BatchServicesStatisticsRequest>
{
    public BatchServicesStatisticsRequestValidator()
    {
        RuleFor(x => x.ServiceIds)
            .NotEmpty()
            .WithMessage("Service IDs list cannot be empty");

        RuleFor(x => x.ServiceIds.Count)
            .ValidBatchSize(100)
            .WithName("Batch Size");

        RuleForEach(x => x.ServiceIds)
            .NotEmptyGuid()
            .WithMessage("All Service IDs must be valid non-empty GUIDs");

        // Check for duplicates
        RuleFor(x => x.ServiceIds)
            .Must(BeUnique)
            .WithMessage("Service IDs list should not contain duplicates");
    }

    private static bool BeUnique(List<Guid> serviceIds)
    {
        return serviceIds.Count == serviceIds.Distinct().Count();
    }
}

/// <summary>
/// Enhanced validator for GetReviewsRequest
/// </summary>
public class GetReviewsRequestValidator : AbstractValidator<GetReviewsRequest>
{
    public GetReviewsRequestValidator()
    {
        RuleFor(x => x.Page)
            .ValidPage();

        RuleFor(x => x.PageSize)
            .ValidPageSize();

        RuleFor(x => x.MinRating)
            .InclusiveBetween(1, 5)
            .When(x => x.MinRating.HasValue)
            .WithMessage("Minimum rating must be between 1 and 5 stars");

        RuleFor(x => x.MaxRating)
            .InclusiveBetween(1, 5)
            .When(x => x.MaxRating.HasValue)
            .WithMessage("Maximum rating must be between 1 and 5 stars");

        RuleFor(x => x.MaxRating)
            .ValidRatingRange(x => x.MinRating);

        RuleFor(x => x.TargetType)
            .IsInEnum()
            .When(x => x.TargetType.HasValue)
            .WithMessage("Target type must be either 'DOCTOR' or 'SERVICE'");

        // Validate optional GUIDs
        RuleFor(x => x.PatientId)
            .NotEmptyGuidWhenHasValue()
            .WithName("Patient ID");

        RuleFor(x => x.DoctorId)
            .NotEmptyGuidWhenHasValue()
            .WithName("Doctor ID");

        RuleFor(x => x.ClinicServiceId)
            .NotEmptyGuidWhenHasValue()
            .WithName("Clinic Service ID");

        // Logical validation
        RuleFor(x => x)
            .Must(BeValidSearchCriteria)
            .WithMessage("At least one filter criteria must be provided (PatientId, DoctorId, ClinicServiceId, or TargetType)")
            .OverridePropertyName("SearchCriteria");
    }

    private static bool BeValidSearchCriteria(GetReviewsRequest request)
    {
        return request.PatientId.HasValue ||
               request.DoctorId.HasValue ||
               request.ClinicServiceId.HasValue ||
               request.TargetType.HasValue ||
               request.MinRating.HasValue ||
               request.MaxRating.HasValue;
    }
}