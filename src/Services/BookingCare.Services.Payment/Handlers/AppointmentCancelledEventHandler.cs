using BookingCare.Services.Payment.Enums;
using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Services.Payment.Models.DTOs.Stripe;
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
            _logger.LogInformation(
                "Payment {PaymentId} uses payment method: {PaymentMethod}, IsStripe: {IsStripe}",
                payment.Id,
                paymentMethod.Name,
                isStripePayment
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
                    _logger.LogInformation(
                        "Patient {PatientId} has active bank account {BankAccountId}. Setting refund status to PENDING.",
                        @event.PatientId,
                        bankAccountId
                    );
                }
                else
                {
                    _logger.LogInformation(
                        "Patient {PatientId} has no active bank account. Setting refund status to WAITING.",
                        @event.PatientId
                    );
                }
            }
            else
            {
                // Stripe payment: Skip bank account validation, automatic refund will be processed
                _logger.LogInformation(
                    "Stripe payment detected. Skipping bank account validation - automatic refund will be processed via Stripe API."
                );
            }

            // Step 3: Calculate refund amount and update payment if needed
            decimal refundAmount;
            string refundReason;

            // Check if this is a doctor change refund (Option 3: Choose new doctor with lower price)
            if (@event.CancellationSource == "DOCTOR_CHANGE_REFUND" && @event.RefundAmount.HasValue)
            {
                // Use exact refund amount from event (price difference)
                refundAmount = @event.RefundAmount.Value;

                // Build detailed refund reason for transparency
                refundReason =
                    $"Hoàn tiền do chuyển bác sĩ: "
                    + $"Từ {@event.OriginalDoctorName} (Cọc: {@event.OriginalConsultationFee:N0} VND) "
                    + $"sang {@event.NewDoctorName} (Cọc: {@event.NewConsultationFee:N0} VND). "
                    + $"Số tiền hoàn lại: {refundAmount:N0} VND";

                _logger.LogInformation(
                    "Doctor change refund for appointment {AppointmentId}: {OldDoctor} → {NewDoctor}, Refund: {RefundAmount}",
                    @event.AppointmentId,
                    @event.OriginalDoctorName,
                    @event.NewDoctorName,
                    refundAmount
                );

                // IMPORTANT: Update payment amount to new doctor's price
                // This prevents incorrect refund calculation if appointment is cancelled again later
                var newDoctorPrice = @event.NewConsultationFee ?? 0;
                if (newDoctorPrice > 0 && payment.Amount != newDoctorPrice)
                {
                    var oldAmount = payment.Amount;
                    payment.Amount = newDoctorPrice;
                    await _paymentRepository.UpdateAsync(payment);

                    _logger.LogInformation(
                        "Updated payment amount for appointment {AppointmentId}: {OldAmount} → {NewAmount} VND (doctor change)",
                        @event.AppointmentId,
                        oldAmount,
                        newDoctorPrice
                    );
                }
            }
            else
            {
                // Regular cancellation refund - calculate based on percentage
                refundAmount = payment.Amount * (@event.RefundPercentage / 100m);
                refundReason = @event.CancellationReason;
            }

            // Step 4: Create refund history record
            var createRefundRequest = new CreateRefundHistoryRequest
            {
                PaymentId = payment.Id,
                UserId = @event.PatientId,
                HospitalId = @event.HospitalId ?? Guid.Empty, // Default to Empty if null
                BankAccountId = bankAccountId,
                RefundAmount = refundAmount,
                RefundReason = refundReason,
            };

            var refundHistory = await _refundHistoryService.CreateAsync(createRefundRequest);

            _logger.LogDebug(
                "Created refund history {RefundHistoryId} with status {Status} for payment {PaymentId}",
                refundHistory.Id,
                refundHistory.Status,
                payment.Id
            );

            // Step 4.5: Process Stripe refund if payment method is Stripe
            if (isStripePayment)
            {
                try
                {
                    _logger.LogInformation(
                        "Initiating automatic Stripe refund for Payment {PaymentId}, RefundHistory {RefundHistoryId}",
                        payment.Id,
                        refundHistory.Id
                    );

                    // Validate PaymentIntentId exists
                    if (string.IsNullOrEmpty(payment.PaymentIntentId))
                    {
                        throw new InvalidOperationException(
                            $"Payment {payment.Id} does not have PaymentIntentId. Cannot process Stripe refund."
                        );
                    }

                    // Create Stripe refund request
                    var stripeRefundRequest = new StripeRefundRequest
                    {
                        PaymentId = payment.Id,
                        Amount = (long)refundAmount, // Convert to cents/smallest currency unit
                        Reason = "requested_by_customer",
                        PaymentIntentId = payment.PaymentIntentId,
                    };

                    // Call Stripe API to process refund
                    var stripeRefundResponse = await _stripeService.CreateRefundAsync(
                        stripeRefundRequest
                    );

                    if (stripeRefundResponse.IsSuccess)
                    {
                        _logger.LogInformation(
                            "Stripe refund succeeded - RefundId: {RefundId}, Status: {Status}, Amount: {Amount}",
                            stripeRefundResponse.RefundId,
                            stripeRefundResponse.Status,
                            stripeRefundResponse.Amount
                        );

                        // Refund history already has COMPLETED status from CreateAsync
                        // Just update StaffNotes with Stripe refund details
                        await _refundHistoryService.UpdateStatusAsync(
                            new UpdateRefundHistoryStatusRequest
                            {
                                Id = refundHistory.Id,
                                Status = RefundStatus.COMPLETED,
                                StaffNotes =
                                    $"Stripe automatic refund completed. RefundId: {stripeRefundResponse.RefundId}",
                            }
                        );

                        // Update payment status to REFUNDED
                        await _paymentService.UpdateStatusAsync(
                            new UpdatePaymentStatusRequest
                            {
                                Id = payment.Id,
                                Status = PaymentStatus.REFUNDED,
                            }
                        );

                        _logger.LogInformation(
                            "Updated payment {PaymentId} to REFUNDED with Stripe RefundId: {RefundId}",
                            payment.Id,
                            stripeRefundResponse.RefundId
                        );
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Stripe refund returned non-success status: {Status} for RefundId: {RefundId}",
                            stripeRefundResponse.Status,
                            stripeRefundResponse.RefundId
                        );

                        // Stripe refund not successful - update to PENDING and add notes
                        // Note: This will fail if ValidateStatusTransition doesn't allow COMPLETED -> PENDING
                        // Consider logging error instead of updating status
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
                        "Failed to process Stripe refund for Payment {PaymentId}, RefundHistory {RefundHistoryId}",
                        payment.Id,
                        refundHistory.Id
                    );

                    // Refund history already COMPLETED - cannot transition to PENDING
                    // Log error for manual intervention instead
                    _logger.LogError(
                        "Stripe refund failed but refund history already COMPLETED. PaymentId: {PaymentId}, RefundHistoryId: {RefundHistoryId}. Manual intervention required.",
                        payment.Id,
                        refundHistory.Id
                    );

                    // Don't throw - allow notification to proceed
                    _logger.LogWarning(
                        "Continuing with refund notification despite Stripe refund failure for RefundHistory {RefundHistoryId}",
                        refundHistory.Id
                    );
                }
            }

            // Step 5: Fetch patient information for notification
            string? patientEmail = null;
            string? patientPhone = null;
            string? patientFullName = null;

            try
            {
                var userRequest = new GetUserBasicInfoRequest { Id = @event.PatientId.ToString() };
                var userResponse = await _userGrpcClient.GetUserBasicInfoAsync(
                    userRequest,
                    cancellationToken: cancellationToken
                );

                patientEmail = userResponse.Email;
                patientPhone = userResponse.Phone;
                patientFullName = $"{userResponse.FirstName} {userResponse.LastName}".Trim();

                _logger.LogDebug(
                    "Fetched patient info for {PatientId}: Email={Email}, Phone={Phone}",
                    @event.PatientId,
                    patientEmail,
                    patientPhone
                );
            }
            catch (Grpc.Core.RpcException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to fetch patient info via gRPC for patient {PatientId}. Proceeding without patient details.",
                    @event.PatientId
                );
            }

            // Step 6: Publish event to Notification Service
            var refundRequestedEvent = new AppointmentRefundRequestedIntegrationEvent
            {
                RefundHistoryId = refundHistory.Id,
                AppointmentId = @event.AppointmentId,
                PatientId = @event.PatientId,
                HospitalId = @event.HospitalId ?? Guid.Empty,
                PaymentId = payment.Id,
                RefundAmount = refundAmount, // Actual refund amount after percentage
                OriginalAmount = payment.Amount, // Original payment amount
                RefundPercentage = @event.RefundPercentage,
                CancellationReason = refundReason, // Use detailed reason for doctor change
                HasBankAccount = hasBankAccount,
                BankAccountId = bankAccountId,
                RefundStatus = refundHistory.Status.ToString(),
                PatientEmail = patientEmail,
                PatientPhone = patientPhone,
                PatientFullName = patientFullName,
                HospitalName = null, // Hospital name not required for notification
                AppointmentDate = @event.AppointmentDate,

                // Doctor change context for notification templates
                CancellationSource = @event.CancellationSource,
                OriginalDoctorName = @event.OriginalDoctorName,
                NewDoctorName = @event.NewDoctorName,
                OriginalConsultationFee = @event.OriginalConsultationFee,
                NewConsultationFee = @event.NewConsultationFee,
            };

            await _eventBus.PublishAsync(
                refundRequestedEvent,
                cancellationToken: cancellationToken
            );

            _logger.LogInformation(
                "Completed refund processing for payment {PaymentId}: Original={Original}, Percentage={Percentage}%, Refund={Refund}, RefundHistoryId={RefundHistoryId}",
                payment.Id,
                payment.Amount,
                @event.RefundPercentage,
                refundAmount,
                refundHistory.Id
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
}
