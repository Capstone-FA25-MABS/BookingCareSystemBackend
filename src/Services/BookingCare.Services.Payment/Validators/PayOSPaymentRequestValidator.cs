using FluentValidation;
using BookingCare.Services.Payment.Models.DTOs.PayOS;

namespace BookingCare.Services.Payment.Validators;

/// <summary>
/// Validator for PayOSPaymentRequest
/// </summary>
public class PayOSPaymentRequestValidator : AbstractValidator<PayOSPaymentRequest>
{
    public PayOSPaymentRequestValidator()
    {
        RuleFor(x => x.PaymentId)
            .NotEmpty()
            .WithMessage("PaymentId must not be empty")
            .NotEqual(Guid.Empty)
            .WithMessage("PaymentId is not valid");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than 0")
            .LessThanOrEqualTo(999999999)
            .WithMessage("Amount must not exceed 999,999,999 VND");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Order description must not be empty")
            .MaximumLength(255)
            .WithMessage("Order description must not exceed 255 characters")
            .Must(BeValidDescription)
            .WithMessage("Order description contains invalid characters");

        // Validate BuyerInfo if present
        When(x => x.BuyerInfo != null, () =>
        {
            RuleFor(x => x.BuyerInfo!.Name)
                .NotEmpty()
                .WithMessage("Buyer name must not be empty when buyer info is provided")
                .MaximumLength(100)
                .WithMessage("Buyer name must not exceed 100 characters");

            RuleFor(x => x.BuyerInfo!.Email)
                .EmailAddress()
                .When(x => !string.IsNullOrEmpty(x.BuyerInfo?.Email))
                .WithMessage("Buyer email is not valid");

            RuleFor(x => x.BuyerInfo!.Phone)
                .Matches(@"^(\+84|84|0)[3-9][0-9]{8}$")
                .When(x => !string.IsNullOrEmpty(x.BuyerInfo?.Phone))
                .WithMessage("Buyer phone number is not valid");
        });

        // Validate Items if present
        When(x => x.Items != null && x.Items.Any(), () =>
        {
            RuleForEach(x => x.Items!)
                .SetValidator(new PayOSItemInfoValidator());

            RuleFor(x => x.Items!)
                .Must(items => items.Sum(i => i.Price * i.Quantity) <= 999999999)
                .WithMessage("Total value of items must not exceed 999,999,999 VND");
        });
    }

    /// <summary>
    /// Check if description contains valid characters
    /// </summary>
    private static bool BeValidDescription(string description)
    {
        if (string.IsNullOrEmpty(description))
            return false;

        // Disallow special characters that may cause issues
        var invalidChars = new[] { '<', '>', '"', '\'', '&', '\n', '\r', '\t' };
        return !description.Any(c => invalidChars.Contains(c));
    }
}

/// <summary>
/// Validator for PayOSItemInfo
/// </summary>
public class PayOSItemInfoValidator : AbstractValidator<PayOSItemInfo>
{
    public PayOSItemInfoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Item name must not be empty")
            .MaximumLength(100)
            .WithMessage("Item name must not exceed 100 characters");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0")
            .LessThanOrEqualTo(1000)
            .WithMessage("Quantity must not exceed 1000");

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage("Item price must be greater than 0")
            .LessThanOrEqualTo(999999999)
            .WithMessage("Item price must not exceed 999,999,999 VND");
    }
}