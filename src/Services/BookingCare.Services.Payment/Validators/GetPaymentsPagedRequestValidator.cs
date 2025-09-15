using FluentValidation;
using BookingCare.Services.Payment.Models.DTOs.Requests;

namespace BookingCare.Services.Payment.Validators;

/// <summary>
/// Validator cho GetPaymentsPagedRequest
/// </summary>
public class GetPaymentsPagedRequestValidator : AbstractValidator<GetPaymentsPagedRequest>
{
    public GetPaymentsPagedRequestValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("PageNumber ph?i l?n h?n 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("PageSize ph?i l?n h?n 0")
            .LessThanOrEqualTo(100)
            .WithMessage("PageSize không ???c v??t quá 100");

        RuleFor(x => x.SortBy)
            .Must(BeValidSortField)
            .WithMessage("SortBy ph?i là m?t trong các giá tr?: CreatedAt, Amount, Status");

        RuleFor(x => x.SortOrder)
            .Must(BeValidSortOrder)
            .WithMessage("SortOrder ph?i là 'asc' ho?c 'desc'");
    }

    private bool BeValidSortField(string sortBy)
    {
        var validSortFields = new[] { "CreatedAt", "Amount", "Status" };
        return validSortFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase);
    }

    private bool BeValidSortOrder(string sortOrder)
    {
        var validSortOrders = new[] { "asc", "desc" };
        return validSortOrders.Contains(sortOrder, StringComparer.OrdinalIgnoreCase);
    }
}