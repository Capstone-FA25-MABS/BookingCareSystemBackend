using BookingCare.Services.Notification.Protos;
using BookingCare.Services.Notification.Utils.OTP;
using Grpc.Core;

namespace BookingCare.Services.Notification.Services;

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

            var verified = await _otpManager.VerifyOtpAsync(request.Key, request.Otp);

            return new VerifyResponse { Verified = verified };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OtpGrpcService.Verify");
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }
}

