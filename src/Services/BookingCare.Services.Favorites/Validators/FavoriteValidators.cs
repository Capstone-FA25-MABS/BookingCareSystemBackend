using FluentValidation;
using BookingCare.Services.Favorites.Models.DTOs;

namespace BookingCare.Services.Favorites.Validators;

/// <summary>
/// Validator for ToggleFavoriteRequest
/// </summary>
public class ToggleFavoriteRequestValidator : AbstractValidator<ToggleFavoriteRequest>
{
    public ToggleFavoriteRequestValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty()
            .WithMessage("Patient ID is required")
            .NotEqual(Guid.Empty)
            .WithMessage("Patient ID cannot be empty");

        RuleFor(x => x.DoctorId)
            .NotEmpty()
            .WithMessage("Doctor ID is required")
            .NotEqual(Guid.Empty)
            .WithMessage("Doctor ID cannot be empty");

        RuleFor(x => x.PatientId)
            .NotEqual(x => x.DoctorId)
            .WithMessage("Patient ID and Doctor ID cannot be the same");
    }
}

/// <summary>
/// Validator for CheckMultipleFavoritesRequest
/// </summary>
public class CheckMultipleFavoritesRequestValidator : AbstractValidator<CheckMultipleFavoritesRequest>
{
    public CheckMultipleFavoritesRequestValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty()
            .WithMessage("Patient ID is required")
            .NotEqual(Guid.Empty)
            .WithMessage("Patient ID cannot be empty");

        RuleFor(x => x.DoctorIds)
            .NotNull()
            .WithMessage("Doctor IDs list is required")
            .NotEmpty()
            .WithMessage("Doctor IDs list cannot be empty")
            .Must(list => list.Count <= 100)
            .WithMessage("Cannot check more than 100 doctors at once");

        RuleForEach(x => x.DoctorIds)
            .NotEqual(Guid.Empty)
            .WithMessage("Doctor ID cannot be empty");

        RuleFor(x => x.DoctorIds)
            .Must(list => list.Distinct().Count() == list.Count)
            .WithMessage("Doctor IDs list cannot contain duplicates");

        RuleFor(x => x)
            .Must(x => !x.DoctorIds.Contains(x.PatientId))
            .WithMessage("Doctor IDs list cannot contain the Patient ID");
    }
}

/// <summary>
/// Validator for GetPatientFavoritesRequest
/// </summary>
public class GetPatientFavoritesRequestValidator : AbstractValidator<GetPatientFavoritesRequest>
{
    public GetPatientFavoritesRequestValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty()
            .WithMessage("Patient ID is required")
            .NotEqual(Guid.Empty)
            .WithMessage("Patient ID cannot be empty");

        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(100)
            .WithMessage("Page size must be between 1 and 100");
    }
}