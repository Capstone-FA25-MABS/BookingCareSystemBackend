using FluentValidation;
using BookingCare.Services.Payment.Models.DTOs.VNPay;
using System;

namespace BookingCare.Services.Payment.Validators;

/// <summary>
/// Validator for VNPayPaymentRequest
/// </summary>
public class VNPayPaymentRequestValidator : AbstractValidator<VNPayPaymentRequest>
{
    public VNPayPaymentRequestValidator()
    {
        RuleFor(x => x.PaymentId)
            .NotEqual(Guid.Empty)
            .WithMessage("PaymentId must not be empty");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than 0")
            .LessThanOrEqualTo(999999999)
            .WithMessage("Amount must not exceed 999,999,999 VND");


        // ClientIP is optional - validate only when provided
        When(x => !string.IsNullOrEmpty(x.ClientIP), () =>
        {
            RuleFor(x => x.ClientIP)
                .Must(BeValidIP)
                .WithMessage("ClientIP must be a valid IP address format");
        });

        // CustomerInfo is also optional
        When(x => !string.IsNullOrEmpty(x.CustomerInfo), () =>
        {
            RuleFor(x => x.CustomerInfo)
                .MaximumLength(50)
                .WithMessage("CustomerInfo must not exceed 50 characters");
        });

        // OrderInfo is optional - validate length when provided
        When(x => !string.IsNullOrEmpty(x.OrderInfo), () =>
        {
            RuleFor(x => x.OrderInfo)
                .MaximumLength(255)
                .WithMessage("OrderInfo must not exceed 255 characters");
        });
    }





    /// <summary>
    /// Validate IP address format (supports IPv4 and development IPs)
    /// </summary>
    private static bool BeValidIP(string? ip)
    {
        if (string.IsNullOrEmpty(ip)) return false;

        // Accept localhost values for development
        if (ip == "127.0.0.1" || ip == "::1" || ip == "localhost")
            return true;

        // Use a short timeout for regex matching to avoid ReDoS
        var timeout = TimeSpan.FromMilliseconds(200);

        // Check IPv4 format
        if (System.Text.RegularExpressions.Regex.IsMatch(ip, @"^(?:[0-9]{1,3}\.){3}[0-9]{1,3}$", System.Text.RegularExpressions.RegexOptions.None, timeout))
        {
            var parts = ip.Split('.');
            return parts.All(part => int.TryParse(part, out var num) && num >= 0 && num <= 255);
        }

        // Check IPv6 basic format (simplified)
        if (ip.Contains(':'))
        {
            return System.Text.RegularExpressions.Regex.IsMatch(ip, @"^[0-9a-fA-F:]+$", System.Text.RegularExpressions.RegexOptions.None, timeout);
        }

        return false;
    }
}