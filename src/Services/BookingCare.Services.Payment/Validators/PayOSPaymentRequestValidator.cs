using FluentValidation;
using BookingCare.Services.Payment.Models.DTOs.PayOS;

namespace BookingCare.Services.Payment.Validators;

/// <summary>
/// Validator cho PayOSPaymentRequest
/// </summary>
public class PayOSPaymentRequestValidator : AbstractValidator<PayOSPaymentRequest>
{
    public PayOSPaymentRequestValidator()
    {
        RuleFor(x => x.PaymentId)
            .NotEmpty()
            .WithMessage("PaymentId không ???c ?? tr?ng")
            .NotEqual(Guid.Empty)
            .WithMessage("PaymentId không h?p l?");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("S? ti?n ph?i l?n h?n 0")
            .LessThanOrEqualTo(999999999)
            .WithMessage("S? ti?n không ???c v??t quá 999,999,999 VN?");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Mô t? ??n hàng không ???c ?? tr?ng")
            .MaximumLength(255)
            .WithMessage("Mô t? ??n hàng không ???c v??t quá 255 ký t?")
            .Must(BeValidDescription)
            .WithMessage("Mô t? ??n hàng ch?a ký t? không h?p l?");

        // Validate BuyerInfo n?u có
        When(x => x.BuyerInfo != null, () =>
        {
            RuleFor(x => x.BuyerInfo!.Name)
                .NotEmpty()
                .WithMessage("Tên ng??i mua không ???c ?? tr?ng khi có thông tin buyer")
                .MaximumLength(100)
                .WithMessage("Tên ng??i mua không ???c v??t quá 100 ký t?");

            RuleFor(x => x.BuyerInfo!.Email)
                .EmailAddress()
                .When(x => !string.IsNullOrEmpty(x.BuyerInfo?.Email))
                .WithMessage("Email ng??i mua không h?p l?");

            RuleFor(x => x.BuyerInfo!.Phone)
                .Matches(@"^(\+84|84|0)[3-9][0-9]{8}$")
                .When(x => !string.IsNullOrEmpty(x.BuyerInfo?.Phone))
                .WithMessage("S? ?i?n tho?i ng??i mua không h?p l?");
        });

        // Validate Items n?u có
        When(x => x.Items != null && x.Items.Any(), () =>
        {
            RuleForEach(x => x.Items!)
                .SetValidator(new PayOSItemInfoValidator());

            RuleFor(x => x.Items!)
                .Must(items => items.Sum(i => i.Price * i.Quantity) <= 999999999)
                .WithMessage("T?ng giá tr? các items không ???c v??t quá 999,999,999 VN?");
        });
    }

    /// <summary>
    /// Ki?m tra mô t? có ch?a ký t? h?p l? không
    /// </summary>
    private bool BeValidDescription(string description)
    {
        if (string.IsNullOrEmpty(description))
            return false;

        // Không ch?a các ký t? ??c bi?t có th? gây l?i
        var invalidChars = new[] { '<', '>', '"', '\'', '&', '\n', '\r', '\t' };
        return !description.Any(c => invalidChars.Contains(c));
    }
}

/// <summary>
/// Validator cho PayOSItemInfo
/// </summary>
public class PayOSItemInfoValidator : AbstractValidator<PayOSItemInfo>
{
    public PayOSItemInfoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Tên s?n ph?m không ???c ?? tr?ng")
            .MaximumLength(100)
            .WithMessage("Tên s?n ph?m không ???c v??t quá 100 ký t?");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("S? l??ng ph?i l?n h?n 0")
            .LessThanOrEqualTo(1000)
            .WithMessage("S? l??ng không ???c v??t quá 1000");

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage("Giá s?n ph?m ph?i l?n h?n 0")
            .LessThanOrEqualTo(999999999)
            .WithMessage("Giá s?n ph?m không ???c v??t quá 999,999,999 VN?");
    }
}