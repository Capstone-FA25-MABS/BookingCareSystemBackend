using BookingCare.Services.Notification.Models.DTOs;
using BookingCare.Services.Notification.Utils.Email;
using BookingCare.Services.Notification.Utils.OTP;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Enums;
using BookingCare.Shared.Common.Services;
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using BookingCare.Services.Auth.Protos;

namespace BookingCare.Services.Notification.Services;

public class OtpService : BaseService, IOtpService
{
    private readonly ManageOtp _otpManager;
    private readonly IEventBus _eventBus;
    private readonly EmailTemplate _templateService;
    private readonly IConfiguration _configuration;
    private readonly AuthService.AuthServiceClient _authClient;

    public OtpService(
        ILogger<OtpService> logger,
        ManageOtp otpManager,
        IEventBus eventBus,
        EmailTemplate templateService,
        IConfiguration configuration,
        AuthService.AuthServiceClient authClient) : base(logger)
    {
        _otpManager = otpManager;
        _eventBus = eventBus;
        _templateService = templateService;
        _configuration = configuration;
        _authClient = authClient;
    }

    public async Task<bool> SendAsync(SendOtpRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.Phone))
            {
                throw new ArgumentException("Either Email or Phone is required");
            }

            var otp = _otpManager.GenerateNumericOtp();
            var purpose = request.Purpose.ToKey();

            // Pre-check for registration
            if (request.Purpose == OtpPurpose.REGISTER)
            {
                var checkRequest = new CheckAccountExistsRequest();
                if (!string.IsNullOrWhiteSpace(request.Email)) checkRequest.Email = request.Email;
                else if (!string.IsNullOrWhiteSpace(request.Phone)) checkRequest.PhoneNumber = request.Phone;
                var checkResponse = await _authClient.CheckAccountExistsAsync(checkRequest);
                if (checkResponse.Exists)
                {
                    return !checkResponse.Exists;
                }
            }

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                await _otpManager.StoreOtpAsync($"purpose:{purpose}:email:{request.Email}", otp, TimeSpan.FromMinutes(5));
                await SendEmailOtp(request.Email, otp, purpose);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(request.DeviceId))
                {
                    throw new ArgumentException("DeviceId is required for SMS OTP");
                }
                await _otpManager.StoreOtpAsync($"purpose:{purpose}:phone:{request.Phone}", otp, TimeSpan.FromMinutes(5));
                await SendSmsOtp(request.Phone!, request.DeviceId!, otp, purpose);
            }

            return true;
        }, "OtpSend");
    }

    public async Task<object> VerifyAsync(VerifyOtpRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.Phone))
            {
                throw new ArgumentException("Either Email or Phone is required");
            }

            var purposeKey = request.Purpose.ToKey();
            var subject = BuildSubject(request.Email, request.Phone);
            var key = $"purpose:{purposeKey}:{subject}";
            var isValid = await _otpManager.VerifyOtpAsync(key, request.Otp);
            if (!isValid)
            {
                throw new InvalidOperationException("Invalid or expired OTP");
            }

            // Set verification flag for Auth service
            var flagKey = $"otp:verified:{purposeKey}:{subject}";
            await _otpManager.StoreOtpAsync(flagKey, "1", TimeSpan.FromMinutes(5));

            // HMAC proof (fallback if Auth can't read Redis)
            string? proof = null;
            long issuedAt = DateTimeOffset.UtcNow.Ticks;
            try
            {
                var secret = _configuration.GetSection("OtpVerification").GetValue<string>("Secret") ?? string.Empty;
                if (!string.IsNullOrEmpty(secret))
                {
                    var data = $"{purposeKey}:{subject}:{issuedAt}";
                    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
                    proof = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)));
                }
            }
            catch (Exception ex)
            {
                LogWarning("Failed generating OTP verification proof: {Message}", ex.Message);
            }

            var channel = !string.IsNullOrWhiteSpace(request.Email) ? "email" : "phone";
            return new { Verified = true, Purpose = purposeKey, Proof = proof, IssuedAt = issuedAt, Channel = channel };
        }, "OtpVerify");
    }

    private async Task SendEmailOtp(string email, string otp, string purpose)
    {
        var emailTitle = "Your One-Time Passcode (OTP)";
        var emailMessage = _templateService.BuildOtpEmailHtml(otp, purpose);

        var @event = new NotificationSendEvent
        {
            UserId = Guid.Empty,
            Title = emailTitle,
            Message = emailMessage,
            Type = "email",
            Data = new Dictionary<string, object>
            {
                { "email", email },
                { "subject", emailTitle },
                { "html", true },
                { "purpose", purpose }
            },
            ScheduledAt = DateTime.UtcNow
        };

        await _eventBus.PublishAsync(@event);
        LogInfo("Email OTP event published for {Email}", null, email);
    }

    private async Task SendSmsOtp(string phone, string deviceId, string otp, string purpose)
    {
        var smsMessage = $"Your OTP is {otp} (valid 5 minutes). Purpose: {purpose}";

        var @event = new NotificationSendEvent
        {
            UserId = Guid.Empty,
            Title = "OTP Verification",
            Message = smsMessage,
            Type = "sms",
            Data = new Dictionary<string, object>
            {
                { "phone", phone },
                { "deviceId", deviceId },
                { "purpose", purpose }
            },
            ScheduledAt = DateTime.UtcNow
        };

        await _eventBus.PublishAsync(@event);
        LogInfo("SMS OTP event published for {Phone} via device {DeviceId}", null, phone, deviceId);
    }

    private static string BuildSubject(string? email, string? phone)
    {
        return !string.IsNullOrWhiteSpace(email) ? $"email:{email}" : $"phone:{phone}";
    }
}

