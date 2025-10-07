using FluentValidation;
using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Services.Payment.Enums;

namespace BookingCare.Services.Payment.Validators;

/// <summary>
/// Validator cho CreateRefundHistoryRequest
/// </summary>
public class CreateRefundHistoryRequestValidator : AbstractValidator<CreateRefundHistoryRequest>
{
    public CreateRefundHistoryRequestValidator()
    {
        RuleFor(x => x.PaymentId)
            .NotEmpty()
            .WithMessage("Payment ID không ???c ?? tr?ng")
            .NotEqual(Guid.Empty)
            .WithMessage("Payment ID không h?p l?");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID không ???c ?? tr?ng")
            .NotEqual(Guid.Empty)
            .WithMessage("User ID không h?p l?");

        RuleFor(x => x.RefundAmount)
            .GreaterThan(0)
            .WithMessage("S? ti?n refund ph?i l?n h?n 0")
            .LessThanOrEqualTo(999999999)
            .WithMessage("S? ti?n refund quá l?n");

        RuleFor(x => x.RefundReason)
            .MaximumLength(500)
            .WithMessage("Lý do refund không ???c v??t quá 500 ký t?")
            .When(x => !string.IsNullOrEmpty(x.RefundReason));

        RuleFor(x => x.BankAccountId)
            .NotEqual(Guid.Empty)
            .WithMessage("Bank Account ID không h?p l?")
            .When(x => x.BankAccountId.HasValue);
    }
}

/// <summary>
/// Validator cho UpdateRefundHistoryStatusRequest
/// </summary>
public class UpdateRefundHistoryStatusRequestValidator : AbstractValidator<UpdateRefundHistoryStatusRequest>
{
    public UpdateRefundHistoryStatusRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("ID không ???c ?? tr?ng")
            .NotEqual(Guid.Empty)
            .WithMessage("ID không h?p l?");

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Tr?ng thái refund không h?p l?");

        RuleFor(x => x.BankAccountId)
            .NotEqual(Guid.Empty)
            .WithMessage("Bank Account ID không h?p l?")
            .When(x => x.BankAccountId.HasValue);

        RuleFor(x => x.StaffNotes)
            .MaximumLength(500)
            .WithMessage("Ghi chú không ???c v??t quá 500 ký t?")
            .When(x => !string.IsNullOrEmpty(x.StaffNotes));

        RuleFor(x => x.ProcessedByStaffId)
            .NotEqual(Guid.Empty)
            .WithMessage("Staff ID không h?p l?")
            .When(x => x.ProcessedByStaffId.HasValue);

        // Business rules validation
        RuleFor(x => x)
            .Must(x => x.Status != RefundStatus.COMPLETED || x.TransferDate.HasValue || x.TransferDate == null)
            .WithMessage("Transfer date s? ???c t? ??ng set khi status = COMPLETED");

        RuleFor(x => x.TransferDate)
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .WithMessage("Transfer date không ???c là t??ng lai")
            .When(x => x.TransferDate.HasValue);
    }
}

/// <summary>
/// Validator cho GetRefundHistoriesRequest
/// </summary>
public class GetRefundHistoriesRequestValidator : AbstractValidator<GetRefundHistoriesRequest>
{
    public GetRefundHistoriesRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty)
            .WithMessage("User ID không h?p l?")
            .When(x => x.UserId.HasValue);

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Tr?ng thái refund không h?p l?")
            .When(x => x.Status.HasValue);

        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("S? trang ph?i l?n h?n 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("Kích th??c trang ph?i l?n h?n 0")
            .LessThanOrEqualTo(100)
            .WithMessage("Kích th??c trang không ???c v??t quá 100");

        RuleFor(x => x.FromDate)
            .LessThanOrEqualTo(x => x.ToDate)
            .WithMessage("T? ngày không ???c l?n h?n ??n ngày")
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue);

        RuleFor(x => x.ToDate)
            .GreaterThanOrEqualTo(x => x.FromDate)
            .WithMessage("??n ngày không ???c nh? h?n t? ngày")
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue);
    }
}