using BookingCare.Services.Payment.Models.DTOs.Requests;
using BookingCare.Services.Payment.Repositories.Interfaces;
using BookingCare.Services.Payment.Services.Interfaces;
using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Services.User.Protos;
using BookingCare.Services.Payment.Enums;

namespace BookingCare.Services.Payment.Handlers;

/// <summary>
/// Handler for AppointmentCancelledIntegrationEvent
/// Creates refund history and triggers notification
/// </summary>
public class AppointmentCancelledEventHandler : IIntegrationEventHandler<AppointmentCancelledIntegrationEvent>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly IRefundHistoryService _refundHistoryService;
    private readonly IEventBus _eventBus;
    private readonly UserService.UserServiceClient _userGrpcClient;
    private readonly ILogger<AppointmentCancelledEventHandler> _logger;

    public AppointmentCancelledEventHandler(
        IPaymentRepository paymentRepository,
        IBankAccountRepository bankAccountRepository,
        IRefundHistoryService refundHistoryService,
        IEventBus eventBus,
        UserService.UserServiceClient userGrpcClient,
        ILogger<AppointmentCancelledEventHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _bankAccountRepository = bankAccountRepository;
        _refundHistoryService = refundHistoryService;
        _eventBus = eventBus;
        _userGrpcClient = userGrpcClient;
        _logger = logger;
    }

    public async Task HandleAsync(AppointmentCancelledIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Processing appointment cancellation for AppointmentId: {AppointmentId}, PatientId: {PatientId}",
                @event.AppointmentId, @event.PatientId);

            // Step 1: Find payment associated with this appointment
            var payment = await _paymentRepository.GetByAppointmentIdAsync(@event.AppointmentId);
            if (payment == null)
            {
                _logger.LogWarning(
                    "No payment found for cancelled appointment {AppointmentId}. Skipping refund process.",
                    @event.AppointmentId);
                return;
            }

            // Only process refund for COMPLETED payments
            if (payment.Status != PaymentStatus.COMPLETED && payment.Status != PaymentStatus.REFUNDED)
            {
                _logger.LogWarning(
                    "Payment {PaymentId} for appointment {AppointmentId} has status {Status}. Skipping refund.",
                    payment.Id, @event.AppointmentId, payment.Status);
                return;
            }

            // Step 2: Check if patient has a bank account for refund
            var defaultBankAccount = await _bankAccountRepository.GetDefaultByUserIdAsync(@event.PatientId);
            Guid? bankAccountId = null;
            var hasBankAccount = false;
            RefundStatus initialStatus;

            if (defaultBankAccount != null && defaultBankAccount.IsActive)
            {
                bankAccountId = defaultBankAccount.Id;
                hasBankAccount = true;
                initialStatus = RefundStatus.PENDING; // Has bank account → PENDING (waiting for staff to process)
                _logger.LogInformation(
                    "Patient {PatientId} has active bank account {BankAccountId}. Setting refund status to PENDING.",
                    @event.PatientId, bankAccountId);
            }
            else
            {
                initialStatus = RefundStatus.WAITING; // No bank account → WAITING (waiting for patient to add bank account)
                _logger.LogInformation(
                    "Patient {PatientId} has no active bank account. Setting refund status to WAITING.",
                    @event.PatientId);
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
                refundReason = $"Hoàn tiền do chuyển bác sĩ: " +
                               $"Từ {@event.OriginalDoctorName} (Cọc: {@event.OriginalConsultationFee:N0} VND) " +
                               $"sang {@event.NewDoctorName} (Cọc: {@event.NewConsultationFee:N0} VND). " +
                               $"Số tiền hoàn lại: {refundAmount:N0} VND";

                _logger.LogInformation(
                    "Doctor change refund for appointment {AppointmentId}: {OldDoctor} → {NewDoctor}, Refund: {RefundAmount}",
                    @event.AppointmentId, @event.OriginalDoctorName, @event.NewDoctorName, refundAmount);

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
                        @event.AppointmentId, oldAmount, newDoctorPrice);
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
                RefundReason = refundReason
            };

            var refundHistory = await _refundHistoryService.CreateAsync(createRefundRequest);

            _logger.LogDebug(
                "Created refund history {RefundHistoryId} with status {Status} for payment {PaymentId}",
                refundHistory.Id, initialStatus, payment.Id);

            // Step 5: Fetch patient information for notification
            string? patientEmail = null;
            string? patientPhone = null;
            string? patientFullName = null;

            try
            {
                var userRequest = new GetUserBasicInfoRequest { Id = @event.PatientId.ToString() };
                var userResponse = await _userGrpcClient.GetUserBasicInfoAsync(userRequest, cancellationToken: cancellationToken);

                patientEmail = userResponse.Email;
                patientPhone = userResponse.Phone;
                patientFullName = $"{userResponse.FirstName} {userResponse.LastName}".Trim();

                _logger.LogDebug(
                    "Fetched patient info for {PatientId}: Email={Email}, Phone={Phone}",
                    @event.PatientId, patientEmail, patientPhone);
            }
            catch (Grpc.Core.RpcException ex)
            {
                _logger.LogWarning(ex,
                    "Failed to fetch patient info via gRPC for patient {PatientId}. Proceeding without patient details.",
                    @event.PatientId);
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
                RefundStatus = initialStatus.ToString(),
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
                NewConsultationFee = @event.NewConsultationFee
            };

            await _eventBus.PublishAsync(refundRequestedEvent, cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Completed refund processing for payment {PaymentId}: Original={Original}, Percentage={Percentage}%, Refund={Refund}, RefundHistoryId={RefundHistoryId}",
                payment.Id, payment.Amount, @event.RefundPercentage, refundAmount, refundHistory.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing appointment cancellation for AppointmentId {AppointmentId}",
                @event.AppointmentId);
            throw new InvalidOperationException(
                $"Failed to process appointment cancellation for AppointmentId {@event.AppointmentId}. See inner exception for details.",
                ex);
        }
    }
}

