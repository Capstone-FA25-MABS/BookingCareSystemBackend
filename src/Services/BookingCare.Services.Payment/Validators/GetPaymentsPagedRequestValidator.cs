using FluentValidation;
using BookingCare.Services.Payment.Models.DTOs.Requests;

namespace BookingCare.Services.Payment.Validators;

/// <summary>
/// Validator for GetPaymentsPagedRequest
/// </summary>
public class GetPaymentsPagedRequestValidator : AbstractValidator<GetPaymentsPagedRequest>
{
    public GetPaymentsPagedRequestValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("PageNumber must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("PageSize must be greater than 0")
            .LessThanOrEqualTo(100)
            .WithMessage("PageSize must not exceed 100");

        RuleFor(x => x.SortBy)
            .Must(BeValidSortField)
            .WithMessage("SortBy must be one of: CreatedAt, Amount, Status");

        RuleFor(x => x.SortOrder)
            .Must(BeValidSortOrder)
            .WithMessage("SortOrder must be 'asc' or 'desc'");
    }

    private static bool BeValidSortField(string sortBy)
    {
        var validSortFields = new[] { "CreatedAt", "Amount", "Status" };
        return validSortFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase);
    }

    private static bool BeValidSortOrder(string sortOrder)
    {
        var validSortOrders = new[] { "asc", "desc" };
        return validSortOrders.Contains(sortOrder, StringComparer.OrdinalIgnoreCase);
    }
}