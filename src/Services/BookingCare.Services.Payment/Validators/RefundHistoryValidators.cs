using FluentValidation;
using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Services.Payment.Enums;

namespace BookingCare.Services.Payment.Validators;

/// <summary>
/// Validator for CreateRefundHistoryRequest
/// </summary>
public class CreateRefundHistoryRequestValidator : AbstractValidator<CreateRefundHistoryRequest>
{
    public CreateRefundHistoryRequestValidator()
    {
        RuleFor(x => x.PaymentId)
            .NotEmpty()
            .WithMessage("Payment ID must not be empty")
            .NotEqual(Guid.Empty)
            .WithMessage("Payment ID is not valid");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID must not be empty")
            .NotEqual(Guid.Empty)
            .WithMessage("User ID is not valid");

        RuleFor(x => x.HospitalId)
            .NotEmpty()
            .WithMessage("Hospital ID must not be empty")
            .NotEqual(Guid.Empty)
            .WithMessage("Hospital ID is not valid");

        RuleFor(x => x.RefundAmount)
            .GreaterThan(0)
            .WithMessage("Refund amount must be greater than 0")
            .LessThanOrEqualTo(999999999)
            .WithMessage("Refund amount is too large");

        RuleFor(x => x.RefundReason)
            .MaximumLength(500)
            .WithMessage("Refund reason must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.RefundReason));

        RuleFor(x => x.BankAccountId)
            .NotEqual(Guid.Empty)
            .WithMessage("Bank Account ID is not valid")
            .When(x => x.BankAccountId.HasValue);
    }
}

/// <summary>
/// Validator for UpdateRefundHistoryStatusRequest
/// </summary>
public class UpdateRefundHistoryStatusRequestValidator : AbstractValidator<UpdateRefundHistoryStatusRequest>
{
    public UpdateRefundHistoryStatusRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Id must not be empty")
            .NotEqual(Guid.Empty)
            .WithMessage("Id is not valid");

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Refund status is not valid");

        RuleFor(x => x.BankAccountId)
            .NotEqual(Guid.Empty)
            .WithMessage("Bank Account ID is not valid")
            .When(x => x.BankAccountId.HasValue);

        RuleFor(x => x.StaffNotes)
            .MaximumLength(500)
            .WithMessage("Staff notes must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.StaffNotes));

        RuleFor(x => x.ProcessedByStaffId)
            .NotEqual(Guid.Empty)
            .WithMessage("Staff ID is not valid")
            .When(x => x.ProcessedByStaffId.HasValue);

        // Business rules validation
        RuleFor(x => x)
            .Must(x => x.Status != RefundStatus.COMPLETED || x.TransferDate.HasValue || x.TransferDate == null)
            .WithMessage("Transfer date will be automatically set when status = COMPLETED");

        RuleFor(x => x.TransferDate)
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .WithMessage("Transfer date must not be in the future")
            .When(x => x.TransferDate.HasValue);
    }
}

/// <summary>
/// Validator for GetRefundHistoriesRequest
/// </summary>
public class GetRefundHistoriesRequestValidator : AbstractValidator<GetRefundHistoriesRequest>
{
    public GetRefundHistoriesRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty)
            .WithMessage("User ID is not valid")
            .When(x => x.UserId.HasValue);

        RuleFor(x => x.HospitalId)
            .NotEqual(Guid.Empty)
            .WithMessage("Hospital ID is not valid")
            .When(x => x.HospitalId.HasValue);

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Refund status is not valid")
            .When(x => x.Status.HasValue);

        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("PageSize must be greater than 0")
            .LessThanOrEqualTo(100)
            .WithMessage("PageSize must not exceed 100");

        RuleFor(x => x.FromDate)
            .LessThanOrEqualTo(x => x.ToDate)
            .WithMessage("FromDate must not be greater than ToDate")
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue);

        RuleFor(x => x.ToDate)
            .GreaterThanOrEqualTo(x => x.FromDate)
            .WithMessage("ToDate must not be less than FromDate")
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue);
    }
}

/// <summary>
/// Validator for MarkAsTransferredRequest
/// </summary>
public class MarkAsTransferredRequestValidator : AbstractValidator<MarkAsTransferredRequest>
{
    public MarkAsTransferredRequestValidator()
    {
        RuleFor(x => x.StaffNotes)
            .MaximumLength(500)
            .WithMessage("Staff notes must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.StaffNotes));
    }
}

/// <summary>
/// Validator for ReportBankIssueRequest
/// </summary>
public class ReportBankIssueRequestValidator : AbstractValidator<ReportBankIssueRequest>
{
    public ReportBankIssueRequestValidator()
    {
        RuleFor(x => x.IssueDescription)
            .NotEmpty()
            .WithMessage("Issue description is required")
            .MaximumLength(1000)
            .WithMessage("Issue description must not exceed 1000 characters");
    }
}