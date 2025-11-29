using BookingCare.Services.Notification.Protos;
using BookingCare.Services.Notification.Utils.OTP;
using BookingCare.Services.Notification.Exceptions;
using BookingCare.Services.Notification.Services.Interfaces;
using BookingCare.Shared.Common.Enums;
using SendOtpRequest = BookingCare.Services.Notification.Protos.SendOtpRequest;
using Status = Grpc.Core.Status;
using Grpc.Core;

namespace BookingCare.Services.Notification.Services.Grpc;

public class OtpGrpcService : OtpVerifier.OtpVerifierBase
{
    private readonly ILogger<OtpGrpcService> _logger;
    private readonly ManageOtp _otpManager;
    private readonly IOtpService _otpService;

    public OtpGrpcService(
        ILogger<OtpGrpcService> logger,
        ManageOtp otpManager,
        IOtpService otpService)
    {
        _logger = logger;
        _otpManager = otpManager;
        _otpService = otpService;
    }

    public override async Task<SendOtpResponse> SendOtp(SendOtpRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC SendOtp called. Email={Email}, Purpose={Purpose}",
                request.Email, request.Purpose);

            // Map purpose string to OtpPurpose enum
            var otpPurpose = MapPurposeToEnum(request.Purpose);

            // Create SendOtpRequest DTO for OtpService
            var sendOtpRequest = new Models.DTOs.SendOtpRequest
            {
                Email = request.Email,
                Phone = request.Phone,
                Purpose = otpPurpose,
                DeviceId = string.Empty // Not needed for email OTP
            };

            // Use existing OtpService logic
            var success = await _otpService.SendAsync(sendOtpRequest);

            var otpKey = !string.IsNullOrWhiteSpace(request.Email)
                ? request.Email
                : request.Phone;

            return new SendOtpResponse
            {
                Success = success,
                Message = success ? "Mã OTP đã được gửi thành công" : "Không thể gửi mã OTP",
                Key = otpKey
            };
        }
        catch (OtpException ex)
        {
            _logger.LogError(ex, "OTP error in gRPC SendOtp. Email={Email}", request.Email);
            return new SendOtpResponse
            {
                Success = false,
                Message = ex.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending OTP. Email={Email}", request.Email);
            return new SendOtpResponse
            {
                Success = false,
                Message = "Không thể gửi mã OTP. Vui lòng thử lại sau."
            };
        }
    }

    public override async Task<VerifyResponse> Verify(VerifyRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC Verify OTP called. Key={Key}", request.Key);

            // Validate input
            if (string.IsNullOrWhiteSpace(request.Key))
            {
                throw new OtpValidationException("Key", "OTP key cannot be null or empty", request.Key);
            }

            if (string.IsNullOrWhiteSpace(request.Otp))
            {
                throw new OtpValidationException("Otp", "OTP code cannot be null or empty", request.Otp);
            }

            var verified = await _otpManager.VerifyOtpAsync(request.Key, request.Otp);

            return new VerifyResponse { Verified = verified };
        }
        catch (OtpException ex)
        {
            _logger.LogError(ex, "OTP error in gRPC Verify. Key={Key}", request.Key);
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in OtpGrpcService.Verify");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Map gRPC purpose string to OtpPurpose enum
    /// </summary>
    private static OtpPurpose MapPurposeToEnum(string purpose)
    {
        return purpose?.ToUpper() switch
        {
            "CONTRACT_SIGNING" => OtpPurpose.CONTRACT_SIGNING,
            "LOGIN" => OtpPurpose.LOGIN,
            "REGISTER" => OtpPurpose.REGISTER,
            "FORGOT_PASSWORD" => OtpPurpose.FORGOT_PASSWORD,
            _ => OtpPurpose.LOGIN // Default to LOGIN
        };
    }

    #endregion
}
