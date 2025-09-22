using FluentValidation;
using BookingCare.Services.Payment.Models.DTOs.VNPay;

namespace BookingCare.Services.Payment.Validators;

/// <summary>
/// Validator cho VNPayPaymentRequest
/// </summary>
public class VNPayPaymentRequestValidator : AbstractValidator<VNPayPaymentRequest>
{
    public VNPayPaymentRequestValidator()
    {
        RuleFor(x => x.PaymentId)
            .NotEqual(Guid.Empty)
            .WithMessage("PaymentId không được để trống");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount phải lớn hơn 0")
            .LessThanOrEqualTo(999999999)
            .WithMessage("Amount không được vượt quá 999,999,999 VND");


        // ClientIP là optional - chỉ validate khi có giá trị
        When(x => !string.IsNullOrEmpty(x.ClientIP), () =>
        {
            RuleFor(x => x.ClientIP)
                .Must(BeValidIP)
                .WithMessage("ClientIP phải có định dạng IP address hợp lệ");
        });

        // CustomerInfo cũng là optional
        When(x => !string.IsNullOrEmpty(x.CustomerInfo), () =>
        {
            RuleFor(x => x.CustomerInfo)
                .MaximumLength(50)
                .WithMessage("CustomerInfo không được vượt quá 50 ký tự")
                .Must(BeValidCustomerInfo)
                .WithMessage("CustomerInfo chứa ký tự không hợp lệ");
        });
    }

    /// <summary>
    /// Validate OrderDescription theo VNPay requirements
    /// </summary>
    private bool BeValidOrderDescription(string? orderDescription)
    {
        if (string.IsNullOrEmpty(orderDescription)) return false;

        // VNPay cho phép: chữ cái, số, khoảng trắng, dấu chấm, dấu gạch ngang, dấu gạch dưới
        return System.Text.RegularExpressions.Regex.IsMatch(orderDescription, @"^[a-zA-Z0-9\s\.\-_]+$");
    }

    /// <summary>
    /// Validate CustomerInfo
    /// </summary>
    private bool BeValidCustomerInfo(string? customerInfo)
    {
        if (string.IsNullOrEmpty(customerInfo)) return true;

        // Chỉ cho phép chữ cái và số
        return System.Text.RegularExpressions.Regex.IsMatch(customerInfo, @"^[a-zA-Z0-9\s]+$");
    }

    /// <summary>
    /// Validate IP address format (supports IPv4 và development IPs)
    /// </summary>
    private bool BeValidIP(string? ip)
    {
        if (string.IsNullOrEmpty(ip)) return false;

        // Accept localhost values for development
        if (ip == "127.0.0.1" || ip == "::1" || ip == "localhost")
            return true;

        // Check IPv4 format
        if (System.Text.RegularExpressions.Regex.IsMatch(ip, @"^(?:[0-9]{1,3}\.){3}[0-9]{1,3}$"))
        {
            var parts = ip.Split('.');
            return parts.All(part => int.TryParse(part, out var num) && num >= 0 && num <= 255);
        }

        // Check IPv6 basic format (simplified)
        if (ip.Contains(':'))
        {
            return System.Text.RegularExpressions.Regex.IsMatch(ip, @"^[0-9a-fA-F:]+$");
        }

        return false;
    }
}