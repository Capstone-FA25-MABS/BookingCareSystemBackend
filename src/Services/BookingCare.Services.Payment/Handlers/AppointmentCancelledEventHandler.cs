using BookingCare.Services.Payment.Enums;
using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Services.Payment.Models.DTOs.Responses;
using BookingCare.Services.Payment.Models.DTOs.Stripe;
using BookingCare.Services.Payment.Models.Entities;
using BookingCare.Services.Payment.Repositories.Interfaces;
using BookingCare.Services.Payment.Services.Interfaces;
using BookingCare.Services.User.Protos;
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;

namespace BookingCare.Services.Payment.Handlers;

/// <summary>
/// Handler for AppointmentCancelledIntegrationEvent
/// Creates refund history and triggers notification
/// Supports automatic Stripe refunds when payment method is Stripe
/// </summary>
public class AppointmentCancelledEventHandler
    : IIntegrationEventHandler<AppointmentCancelledIntegrationEvent>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentMethodRepository _paymentMethodRepository;
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly IRefundHistoryService _refundHistoryService;
    private readonly IStripeService _stripeService;
    private readonly IPaymentService _paymentService;
    private readonly IEventBus _eventBus;
    private readonly UserService.UserServiceClient _userGrpcClient;
    private readonly ILogger<AppointmentCancelledEventHandler> _logger;

    public AppointmentCancelledEventHandler(
        IPaymentRepository paymentRepository,
        IPaymentMethodRepository paymentMethodRepository,
        IBankAccountRepository bankAccountRepository,
        IRefundHistoryService refundHistoryService,
        IStripeService stripeService,
        IPaymentService paymentService,
        IEventBus eventBus,
        UserService.UserServiceClient userGrpcClient,
        ILogger<AppointmentCancelledEventHandler> logger
    )
    {
        _paymentRepository = paymentRepository;
        _paymentMethodRepository = paymentMethodRepository;
        _bankAccountRepository = bankAccountRepository;
        _refundHistoryService = refundHistoryService;
        _stripeService = stripeService;
        _paymentService = paymentService;
        _eventBus = eventBus;
        _userGrpcClient = userGrpcClient;
        _logger = logger;
    }

    public async Task HandleAsync(
        AppointmentCancelledIntegrationEvent @event,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            _logger.LogInformation(
                "Processing appointment cancellation for AppointmentId: {AppointmentId}, PatientId: {PatientId}",
                @event.AppointmentId,
                @event.PatientId
            );

            // Step 1: Find payment associated with this appointment
            var payment = await _paymentRepository.GetByAppointmentIdAsync(@event.AppointmentId);
            if (payment == null)
            {
                _logger.LogWarning(
                    "No payment found for cancelled appointment {AppointmentId}. Skipping refund process.",
                    @event.AppointmentId
                );
                return;
            }

            // Only process refund for COMPLETED payments
            if (
                payment.Status != PaymentStatus.COMPLETED
                && payment.Status != PaymentStatus.REFUNDED
            )
            {
                _logger.LogWarning(
                    "Payment {PaymentId} for appointment {AppointmentId} has status {Status}. Skipping refund.",
                    payment.Id,
                    @event.AppointmentId,
                    payment.Status
                );
                return;
            }

            // Step 2: Check payment method to determine refund strategy
            var paymentMethod = await _paymentMethodRepository.GetByIdAsync(
                payment.PaymentMethodId
            );
            if (paymentMethod == null)
            {
                _logger.LogError(
                    "Payment method not found for PaymentMethodId: {PaymentMethodId}. Cannot process refund.",
                    payment.PaymentMethodId
                );
                throw new InvalidOperationException(
                    $"Payment method {payment.PaymentMethodId} not found"
                );
            }

            bool isStripePayment = paymentMethod.Name.Equals(
                "STRIPE",
                StringComparison.OrdinalIgnoreCase
            );

            Guid? bankAccountId = null;
            var hasBankAccount = isStripePayment; // Assume true for Stripe payments

            if (!isStripePayment)
            {
                // Non-Stripe payment: Check if patient has a bank account for manual refund
                var defaultBankAccount = await _bankAccountRepository.GetDefaultByUserIdAsync(
                    @event.PatientId
                );

                if (defaultBankAccount != null && defaultBankAccount.IsActive)
                {
                    bankAccountId = defaultBankAccount.Id;
                    hasBankAccount = true;
                }

                _logger.LogInformation(
                    "Processing refund for Payment {PaymentId} using {PaymentMethod} | Patient {PatientId} bank account: HasActive={HasActive}, BankAccountId={BankAccountId}, RefundStatus={RefundStatus}",
                    payment.Id,
                    paymentMethod.Name,
                    @event.PatientId,
                    hasBankAccount,
                    bankAccountId,
                    hasBankAccount ? "PENDING" : "WAITING"
                );
            }
            else
            {
                // Stripe payment: Skip bank account validation, automatic refund will be processed
                _logger.LogInformation(
                    "Processing refund for Payment {PaymentId} using {PaymentMethod} | Stripe payment detected - automatic refund will be processed via Stripe API",
                    payment.Id,
                    paymentMethod.Name
                );
            }

            // Step 3: Calculate refund amount and update payment if needed
            var (refundAmount, refundReason) = await CalculateRefundAndUpdatePaymentAsync(
                payment,
                @event
            );

            // Step 4: Create refund history record
            var refundHistory = await CreateRefundHistoryAsync(
                payment.Id,
                @event.PatientId,
                @event.HospitalId,
                bankAccountId,
                refundAmount,
                refundReason
            );

            // Step 4.5: Process Stripe refund if payment method is Stripe
            if (isStripePayment)
            {
                await ProcessStripeAutomaticRefundAsync(payment, refundHistory);
            }

            // Step 5: Fetch patient information and publish notification event
            await PublishRefundNotificationAsync(
                @event,
                payment,
                refundHistory,
                refundAmount,
                refundReason,
                hasBankAccount,
                bankAccountId,
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing appointment cancellation for AppointmentId {AppointmentId}",
                @event.AppointmentId
            );
            throw new InvalidOperationException(
                $"Failed to process appointment cancellation for AppointmentId {@event.AppointmentId}. See inner exception for details.",
                ex
            );
        }
    }

    private async Task<(
        decimal refundAmount,
        string refundReason
    )> CalculateRefundAndUpdatePaymentAsync(
        PaymentEntity payment,
        AppointmentCancelledIntegrationEvent @event
    )
    {
        decimal refundAmount;
        string refundReason;

        if (@event.CancellationSource == "DOCTOR_CHANGE_REFUND" && @event.RefundAmount.HasValue)
        {
            refundAmount = @event.RefundAmount.Value;
            refundReason =
                $"Hoàn tiền do chuyển bác sĩ: Từ {@event.OriginalDoctorName} (Cọc: {@event.OriginalConsultationFee:N0} VND) sang {@event.NewDoctorName} (Cọc: {@event.NewConsultationFee:N0} VND). Số tiền hoàn lại: {refundAmount:N0} VND";

            await UpdatePaymentForDoctorChangeAsync(payment, @event, refundAmount);
        }
        else
        {
            refundAmount = payment.Amount * (@event.RefundPercentage / 100m);
            refundReason = @event.CancellationReason;
        }

        return (refundAmount, refundReason);
    }

    private async Task UpdatePaymentForDoctorChangeAsync(
        PaymentEntity payment,
        AppointmentCancelledIntegrationEvent @event,
        decimal refundAmount
    )
    {
        var newDoctorPrice = @event.NewConsultationFee ?? 0;
        if (newDoctorPrice > 0 && payment.Amount != newDoctorPrice)
        {
            var oldAmount = payment.Amount;
            payment.Amount = newDoctorPrice;
            await _paymentRepository.UpdateAsync(payment);

            _logger.LogInformation(
                "Doctor change refund for appointment {AppointmentId}: {OldDoctor} → {NewDoctor}, Refund: {RefundAmount} | Updated payment amount: {OldAmount} → {NewAmount} VND",
                @event.AppointmentId,
                @event.OriginalDoctorName,
                @event.NewDoctorName,
                refundAmount,
                oldAmount,
                newDoctorPrice
            );
        }
        else
        {
            _logger.LogInformation(
                "Doctor change refund for appointment {AppointmentId}: {OldDoctor} → {NewDoctor}, Refund: {RefundAmount}",
                @event.AppointmentId,
                @event.OriginalDoctorName,
                @event.NewDoctorName,
                refundAmount
            );
        }
    }

    private async Task<RefundHistoryResponse> CreateRefundHistoryAsync(
        Guid paymentId,
        Guid userId,
        Guid? hospitalId,
        Guid? bankAccountId,
        decimal refundAmount,
        string refundReason
    )
    {
        var createRefundRequest = new CreateRefundHistoryRequest
        {
            PaymentId = paymentId,
            UserId = userId,
            HospitalId = hospitalId ?? Guid.Empty,
            BankAccountId = bankAccountId,
            RefundAmount = refundAmount,
            RefundReason = refundReason,
        };

        var refundHistory = await _refundHistoryService.CreateAsync(createRefundRequest);
        _logger.LogDebug(
            "Created refund history {RefundHistoryId} with status {Status} for payment {PaymentId}",
            refundHistory.Id,
            refundHistory.Status,
            paymentId
        );

        return refundHistory;
    }

    private async Task ProcessStripeAutomaticRefundAsync(
        PaymentEntity payment,
        RefundHistoryResponse refundHistory
    )
    {
        try
        {
            _logger.LogInformation(
                "Initiating automatic Stripe refund for Payment {PaymentId}, RefundHistory {RefundHistoryId}",
                payment.Id,
                refundHistory.Id
            );

            if (string.IsNullOrEmpty(payment.PaymentIntentId))
            {
                throw new InvalidOperationException(
                    $"Payment {payment.Id} does not have PaymentIntentId. Cannot process Stripe refund."
                );
            }

            var stripeRefundRequest = new StripeRefundRequest
            {
                PaymentId = payment.Id,
                Amount = (long)refundHistory.RefundAmount,
                Reason = "requested_by_customer",
                PaymentIntentId = payment.PaymentIntentId,
            };

            var stripeRefundResponse = await _stripeService.CreateRefundAsync(stripeRefundRequest);

            if (stripeRefundResponse.IsSuccess)
            {
                await HandleSuccessfulStripeRefundAsync(
                    payment,
                    refundHistory,
                    stripeRefundResponse
                );
            }
            else
            {
                _logger.LogError(
                    "Stripe refund failed but refund history already COMPLETED. PaymentId: {PaymentId}, RefundHistoryId: {RefundHistoryId}. Manual intervention required.",
                    payment.Id,
                    refundHistory.Id
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Stripe refund failed for Payment {PaymentId}, RefundHistory {RefundHistoryId} - already COMPLETED, manual intervention required",
                payment.Id,
                refundHistory.Id
            );
            _logger.LogWarning(
                "Continuing with refund notification despite Stripe refund failure for RefundHistory {RefundHistoryId}",
                refundHistory.Id
            );
        }
    }

    private async Task HandleSuccessfulStripeRefundAsync(
        PaymentEntity payment,
        RefundHistoryResponse refundHistory,
        StripeRefundResponse stripeRefundResponse
    )
    {
        _logger.LogInformation(
            "Stripe refund succeeded - RefundId: {RefundId}, Status: {Status}, Amount: {Amount}",
            stripeRefundResponse.RefundId,
            stripeRefundResponse.Status,
            stripeRefundResponse.Amount
        );

        await _refundHistoryService.UpdateStatusAsync(
            new UpdateRefundHistoryStatusRequest
            {
                Id = refundHistory.Id,
                Status = RefundStatus.COMPLETED,
                StaffNotes =
                    $"Stripe automatic refund completed. RefundId: {stripeRefundResponse.RefundId}",
            }
        );

        await _paymentService.UpdateStatusAsync(
            new UpdatePaymentStatusRequest { Id = payment.Id, Status = PaymentStatus.REFUNDED }
        );

        _logger.LogInformation(
            "Updated payment {PaymentId} to REFUNDED with Stripe RefundId: {RefundId}",
            payment.Id,
            stripeRefundResponse.RefundId
        );
    }

    private async Task PublishRefundNotificationAsync(
        AppointmentCancelledIntegrationEvent @event,
        PaymentEntity payment,
        RefundHistoryResponse refundHistory,
        decimal refundAmount,
        string refundReason,
        bool hasBankAccount,
        Guid? bankAccountId,
        CancellationToken cancellationToken
    )
    {
        var (patientEmail, patientPhone, patientFullName) = await FetchPatientInfoAsync(
            @event.PatientId,
            cancellationToken
        );

        var refundRequestedEvent = new AppointmentRefundRequestedIntegrationEvent
        {
            RefundHistoryId = refundHistory.Id,
            AppointmentId = @event.AppointmentId,
            PatientId = @event.PatientId,
            HospitalId = @event.HospitalId ?? Guid.Empty,
            PaymentId = payment.Id,
            RefundAmount = refundAmount,
            OriginalAmount = payment.Amount,
            RefundPercentage = @event.RefundPercentage,
            CancellationReason = refundReason,
            HasBankAccount = hasBankAccount,
            BankAccountId = bankAccountId,
            RefundStatus = refundHistory.Status.ToString(),
            PatientEmail = patientEmail,
            PatientPhone = patientPhone,
            PatientFullName = patientFullName,
            HospitalName = null,
            AppointmentDate = @event.AppointmentDate,
            CancellationSource = @event.CancellationSource,
            OriginalDoctorName = @event.OriginalDoctorName,
            NewDoctorName = @event.NewDoctorName,
            OriginalConsultationFee = @event.OriginalConsultationFee,
            NewConsultationFee = @event.NewConsultationFee,
        };

        _logger.LogInformation(
            "Publishing refund request event for payment {PaymentId}: Original={Original}, Percentage={Percentage}%, Refund={Refund}, RefundHistoryId={RefundHistoryId}",
            payment.Id,
            payment.Amount,
            @event.RefundPercentage,
            refundAmount,
            refundHistory.Id
        );

        await _eventBus.PublishAsync(refundRequestedEvent, cancellationToken: cancellationToken);
    }

    private async Task<(string? email, string? phone, string? fullName)> FetchPatientInfoAsync(
        Guid patientId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var userRequest = new GetUserBasicInfoRequest { Id = patientId.ToString() };
            var userResponse = await _userGrpcClient.GetUserBasicInfoAsync(
                userRequest,
                cancellationToken: cancellationToken
            );

            var email = userResponse.Email;
            var phone = userResponse.Phone;
            var fullName = $"{userResponse.FirstName} {userResponse.LastName}".Trim();

            _logger.LogDebug(
                "Fetched patient info for {PatientId}: Email={Email}, Phone={Phone}",
                patientId,
                email,
                phone
            );
            return (email, phone, fullName);
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to fetch patient info via gRPC for patient {PatientId}. Proceeding without patient details.",
                patientId
            );
            return (null, null, null);
        }
    }
}
