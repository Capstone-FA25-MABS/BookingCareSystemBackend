using FluentValidation;
using BookingCare.Services.Payment.Models.DTOs.Requests;

namespace BookingCare.Services.Payment.Validators;

/// <summary>
/// Validator for GetPaymentStatisticsRequest
/// </summary>
public class GetPaymentStatisticsRequestValidator : AbstractValidator<GetPaymentStatisticsRequest>
{
    public GetPaymentStatisticsRequestValidator()
    {
        // Validate FromDate if provided
        When(x => x.FromDate.HasValue, () =>
        {
            RuleFor(x => x.FromDate)
                .LessThanOrEqualTo(DateTime.Now)
                .WithMessage("FromDate must not be greater than current date");
        });

        // Validate ToDate if provided
        When(x => x.ToDate.HasValue, () =>
        {
            RuleFor(x => x.ToDate)
                .LessThanOrEqualTo(DateTime.Now)
                .WithMessage("ToDate must not be greater than current date");
        });

        // Validate date range if both provided
        When(x => x.FromDate.HasValue && x.ToDate.HasValue, () =>
        {
            RuleFor(x => x.FromDate)
                .LessThan(x => x.ToDate)
                .WithMessage("FromDate must be earlier than ToDate");
        });

        RuleFor(x => x.Period)
            .IsInEnum()
            .WithMessage("Period is not valid");

        // Validate ClinicId if provided
        When(x => x.ClinicId.HasValue, () =>
        {
            RuleFor(x => x.ClinicId)
                .NotEqual(Guid.Empty)
                .WithMessage("ClinicId must not be empty if provided");
        });

        // Validate PatientId if provided
        When(x => x.PatientId.HasValue, () =>
        {
            RuleFor(x => x.PatientId)
                .NotEqual(Guid.Empty)
                .WithMessage("PatientId must not be empty if provided");
        });

        // Validate TransactionType if provided
        When(x => !string.IsNullOrEmpty(x.TransactionType), () =>
        {
            RuleFor(x => x.TransactionType)
                .Must(BeValidTransactionType)
                .WithMessage("TransactionType must be 'APPOINTMENT' or 'SUBSCRIPTION'");
        });

        // Validate Status if provided
        When(x => !string.IsNullOrEmpty(x.Status), () =>
        {
            RuleFor(x => x.Status)
                .Must(BeValidStatus)
                .WithMessage("Status must be one of: PENDING, COMPLETED, FAILED, REFUNDED");
        });

        // Validate date range is not too large (uses computed dates)
        RuleFor(x => x)
            .Must(HaveReasonableDateRange)
            .WithMessage("Statistics date range must not exceed 5 years");
    }

    private bool BeValidTransactionType(string? transactionType)
    {
        if (string.IsNullOrEmpty(transactionType)) return true;
        var validTypes = new[] { "APPOINTMENT", "SUBSCRIPTION" };
        return validTypes.Contains(transactionType, StringComparer.OrdinalIgnoreCase);
    }

    private bool BeValidStatus(string? status)
    {
        if (string.IsNullOrEmpty(status)) return true;
        var validStatuses = new[] { "PENDING", "COMPLETED", "FAILED", "REFUNDED" };
        return validStatuses.Contains(status, StringComparer.OrdinalIgnoreCase);
    }

    private bool HaveReasonableDateRange(GetPaymentStatisticsRequest request)
    {
        var fromDate = request.GetFromDate();
        var toDate = request.GetToDate();
        var dateRange = toDate - fromDate;
        return dateRange.TotalDays <= 365 * 5; // Max 5 years
    }
}