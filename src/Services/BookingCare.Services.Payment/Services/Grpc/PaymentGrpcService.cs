using Grpc.Core;
using BookingCare.Services.Payment.Protos;
using BookingCare.Services.Payment.Services.Interfaces;

namespace BookingCare.Services.Payment.Services.Grpc;

/// <summary>
/// gRPC service for Payment operations
/// </summary>
public class PaymentGrpcService : PaymentService.PaymentServiceBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentGrpcService> _logger;

    public PaymentGrpcService(
        IPaymentService paymentService,
        ILogger<PaymentGrpcService> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Get payment information by appointment ID
    /// </summary>
    public override async Task<GetPaymentByAppointmentIdResponse> GetPaymentByAppointmentId(
        GetPaymentByAppointmentIdRequest request,
        ServerCallContext context)
    {
        try
        {
            LogInfo("Getting payment for appointment {AppointmentId}", null, request.AppointmentId);

            if (string.IsNullOrEmpty(request.AppointmentId) || !Guid.TryParse(request.AppointmentId, out var appointmentId))
            {
                return new GetPaymentByAppointmentIdResponse
                {
                    Success = false,
                    Message = "Invalid appointment ID format"
                };
            }

            // Get payment by appointment ID
            var payment = await _paymentService.GetByAppointmentIdAsync(appointmentId);

            if (payment == null)
            {
                LogInfo("No payment found for appointment {AppointmentId}", null, request.AppointmentId);
                return new GetPaymentByAppointmentIdResponse
                {
                    Success = false,
                    Message = "Payment not found for this appointment"
                };
            }

            // Map to gRPC response
            var response = new GetPaymentByAppointmentIdResponse
            {
                Success = true,
                Message = "Payment retrieved successfully",
                Payment = new PaymentInfo
                {
                    Id = payment.Id.ToString(),
                    AppointmentId = payment.AppointmentId?.ToString() ?? "",
                    Amount = (double)payment.Amount,
                }
            };

            LogInfo("Successfully retrieved payment {PaymentId} for appointment {AppointmentId}",
                null, payment.Id, request.AppointmentId);

            return response;
        }
        catch (Exception ex)
        {
            LogError(ex, "Error getting payment for appointment {AppointmentId}", null, request.AppointmentId);

            return new GetPaymentByAppointmentIdResponse
            {
                Success = false,
                Message = "Internal server error occurred while retrieving payment information"
            };
        }
    }

    #region IBaseService Implementation

    public void LogInfo(string message, string? correlationId, params object[] args)
    {
        _logger.LogInformation(message, args);
    }

    public void LogError(Exception exception, string message, string? correlationId, params object[] args)
    {
        _logger.LogError(exception, message, args);
    }

    public void LogWarning(string message, string? correlationId, params object[] args)
    {
        _logger.LogWarning(message, args);
    }

    public void LogDebug(string message, string? correlationId, params object[] args)
    {
        _logger.LogDebug(message, args);
    }

    #endregion
}
