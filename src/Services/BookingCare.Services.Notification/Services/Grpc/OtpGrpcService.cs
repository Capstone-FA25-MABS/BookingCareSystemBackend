using BookingCare.Services.Notification.Protos;
using BookingCare.Services.Notification.Utils.OTP;
using BookingCare.Services.Notification.Exceptions;
using Grpc.Core;

namespace BookingCare.Services.Notification.Services.Grpc;

public class OtpGrpcService : OtpVerifier.OtpVerifierBase
{
    private readonly ILogger<OtpGrpcService> _logger;
    private readonly ManageOtp _otpManager;

    public OtpGrpcService(ILogger<OtpGrpcService> logger, ManageOtp otpManager)
    {
        _logger = logger;
        _otpManager = otpManager;
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
}
