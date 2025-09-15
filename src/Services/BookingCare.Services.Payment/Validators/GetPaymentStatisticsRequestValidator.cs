using FluentValidation;
using BookingCare.Services.Payment.Models.DTOs.Requests;

namespace BookingCare.Services.Payment.Validators;

/// <summary>
/// Validator cho GetPaymentStatisticsRequest
/// </summary>
public class GetPaymentStatisticsRequestValidator : AbstractValidator<GetPaymentStatisticsRequest>
{
    public GetPaymentStatisticsRequestValidator()
    {
        // Validate FromDate n?u có giá tr?
        When(x => x.FromDate.HasValue, () =>
        {
            RuleFor(x => x.FromDate)
                .LessThanOrEqualTo(DateTime.Now)
                .WithMessage("FromDate không ???c l?n h?n ngày hi?n t?i");
        });

        // Validate ToDate n?u có giá tr?
        When(x => x.ToDate.HasValue, () =>
        {
            RuleFor(x => x.ToDate)
                .LessThanOrEqualTo(DateTime.Now)
                .WithMessage("ToDate không ???c l?n h?n ngày hi?n t?i");
        });

        // Validate date range n?u c? hai ??u có giá tr?
        When(x => x.FromDate.HasValue && x.ToDate.HasValue, () =>
        {
            RuleFor(x => x.FromDate)
                .LessThan(x => x.ToDate)
                .WithMessage("FromDate ph?i nh? h?n ToDate");
        });

        RuleFor(x => x.Period)
            .IsInEnum()
            .WithMessage("Period không h?p l?");

        // Validate ClinicId n?u có giá tr?
        When(x => x.ClinicId.HasValue, () =>
        {
            RuleFor(x => x.ClinicId)
                .NotEqual(Guid.Empty)
                .WithMessage("ClinicId không ???c ?? tr?ng n?u ???c cung c?p");
        });

        // Validate PatientId n?u có giá tr?
        When(x => x.PatientId.HasValue, () =>
        {
            RuleFor(x => x.PatientId)
                .NotEqual(Guid.Empty)
                .WithMessage("PatientId không ???c ?? tr?ng n?u ???c cung c?p");
        });

        // Validate TransactionType n?u có giá tr?
        When(x => !string.IsNullOrEmpty(x.TransactionType), () =>
        {
            RuleFor(x => x.TransactionType)
                .Must(BeValidTransactionType)
                .WithMessage("TransactionType ph?i là 'APPOINTMENT' ho?c 'SUBSCRIPTION'");
        });

        // Validate Status n?u có giá tr?
        When(x => !string.IsNullOrEmpty(x.Status), () =>
        {
            RuleFor(x => x.Status)
                .Must(BeValidStatus)
                .WithMessage("Status ph?i là m?t trong các giá tr?: PENDING, COMPLETED, FAILED, REFUNDED");
        });

        // Validate date range không quá l?n (s? d?ng computed dates)
        RuleFor(x => x)
            .Must(HaveReasonableDateRange)
            .WithMessage("Kho?ng th?i gian th?ng kê không ???c v??t quá 5 n?m");
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
        return dateRange.TotalDays <= 365 * 5; // T?i ?a 5 n?m
    }
}