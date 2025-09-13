using BookingCare.Shared.EventBus.Events;
using BookingCare.Shared.Saga.Abstractions;
using BookingCare.Shared.Saga.Core;
using BookingCare.Shared.Saga.Models;

namespace BookingCare.Services.Appointment.Sagas;

/// <summary>
/// Comprehensive appointment lifecycle saga
/// Handles: Creation, Confirmation, Modification, Cancellation, Follow-up
/// </summary>
public class AppointmentLifecycleSaga : SagaDefinitionBase
{
    public override string SagaName => "AppointmentLifecycle";
    public override TimeSpan GlobalTimeout => TimeSpan.FromMinutes(30);

    public AppointmentLifecycleSaga()
    {
        // Main appointment flow
        AddStep<ValidateAppointmentRequestStep>();
        AddStep<CheckDoctorAvailabilityStep>();
        AddStep<ValidatePatientEligibilityStep>();
        AddStep<ReserveAppointmentSlotStep>();
        AddStep<ProcessAppointmentPaymentStep>();
        AddStep<CreateAppointmentRecordStep>();
        AddStep<SendAppointmentConfirmationStep>();
        AddStep<UpdateDoctorScheduleStep>();
        AddStep<CreateCalendarEventStep>();
        AddStep<NotifyRelatedPartiesStep>();
    }
}

/// <summary>
/// Saga for appointment rescheduling
/// </summary>
public class AppointmentReschedulingSaga : SagaDefinitionBase
{
    public override string SagaName => "AppointmentRescheduling";
    public override TimeSpan GlobalTimeout => TimeSpan.FromMinutes(20);

    public AppointmentReschedulingSaga()
    {
        // Note: These steps need to be implemented
        // AddStep<ValidateRescheduleRequestStep>();
        // AddStep<CheckNewSlotAvailabilityStep>();
        // AddStep<ReleaseOriginalSlotStep>();
        // AddStep<ReserveNewSlotStep>();
        // AddStep<UpdateAppointmentRecordStep>();
        // AddStep<ProcessReschedulingFeesStep>();
        // AddStep<SendRescheduleNotificationStep>();
        // AddStep<UpdateCalendarEventsStep>();
    }
}

// Saga Steps Implementation
public class ValidateAppointmentRequestStep : CompensatableSagaStepBase
{
    public override string StepName => "ValidateAppointmentRequest";
    public override int Order => 1;

    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var patientId = context.GetData<string>("PatientId");
        var doctorId = context.GetData<string>("DoctorId");
        var appointmentDate = context.GetData<DateTime>("AppointmentDate");
        var serviceType = context.GetData<string>("ServiceType");

        await Task.Delay(100, cancellationToken);

        // Validate required fields
        if (string.IsNullOrEmpty(patientId))
            return Failure("Patient ID is required");

        if (string.IsNullOrEmpty(doctorId))
            return Failure("Doctor ID is required");

        if (appointmentDate <= DateTime.UtcNow)
            return Failure("Appointment date must be in the future");

        if (string.IsNullOrEmpty(serviceType))
            return Failure("Service type is required");

        // Validate appointment timing (business hours, etc.)
        if (appointmentDate.Hour < 8 || appointmentDate.Hour >= 18)
            return Failure("Appointments can only be scheduled between 8 AM and 6 PM");

        context.SetData("RequestValidated", true);
        context.SetData("ValidationTimestamp", DateTime.UtcNow);

        return Success();
    }

    public override async Task<SagaStepResult> CompensateAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        // No compensation needed for validation
        await Task.Delay(1, cancellationToken);
        return Success();
    }
}

public class CheckDoctorAvailabilityStep : CompensatableSagaStepBase
{
    public override string StepName => "CheckDoctorAvailability";
    public override int Order => 2;

    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var doctorId = context.GetData<string>("DoctorId");
        var appointmentDate = context.GetData<DateTime>("AppointmentDate");
        var serviceType = context.GetData<string>("ServiceType");

        if (string.IsNullOrEmpty(doctorId))
            return Failure("DoctorId is required");
            
        if (string.IsNullOrEmpty(serviceType))
            return Failure("ServiceType is required");

        await Task.Delay(200, cancellationToken);

        // Check doctor's schedule
        // This would typically call the Doctor Service API
        var isDoctorAvailable = await CheckDoctorSchedule(doctorId, appointmentDate);
        
        if (!isDoctorAvailable)
            return Failure("Doctor is not available at the requested time");

        // Check if doctor provides the requested service
        var providesService = await CheckDoctorServices(doctorId, serviceType);
        
        if (!providesService)
            return Failure($"Doctor does not provide {serviceType} service");

        context.SetData("DoctorAvailable", true);
        context.SetData("ServiceCompatible", true);

        return Success();
    }

    public override async Task<SagaStepResult> CompensateAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        // No compensation needed for availability check
        await Task.Delay(1, cancellationToken);
        return Success();
    }

    private async Task<bool> CheckDoctorSchedule(string doctorId, DateTime appointmentDate)
    {
        // Simulate doctor service call
        await Task.Delay(50);
        return DateTime.UtcNow.Millisecond % 3 != 0; // 66% availability
    }

    private async Task<bool> CheckDoctorServices(string doctorId, string serviceType)
    {
        // Simulate service compatibility check
        await Task.Delay(30);
        return true; // Assume all doctors provide all services for now
    }
}

public class ValidatePatientEligibilityStep : CompensatableSagaStepBase
{
    public override string StepName => "ValidatePatientEligibility";
    public override int Order => 3;

    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var patientId = context.GetData<string>("PatientId");
        var serviceType = context.GetData<string>("ServiceType");

        if (string.IsNullOrEmpty(patientId))
            return Failure("PatientId is required");
        
        if (string.IsNullOrEmpty(serviceType))
            return Failure("ServiceType is required");

        await Task.Delay(150, cancellationToken);

        // Check patient's medical history and eligibility
        var patientExists = await ValidatePatientExists(patientId);
        if (!patientExists)
            return Failure("Patient record not found");

        // Check if patient has any restrictions for this service
        var hasRestrictions = await CheckPatientRestrictions(patientId, serviceType);
        if (hasRestrictions)
            return Failure("Patient has medical restrictions for this service type");

        // Check insurance coverage if applicable
        var insuranceCoverage = await CheckInsuranceCoverage(patientId, serviceType);
        context.SetData("InsuranceCovered", insuranceCoverage);

        context.SetData("PatientEligible", true);
        return Success();
    }

    public override async Task<SagaStepResult> CompensateAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        // No compensation needed for eligibility check
        await Task.Delay(1, cancellationToken);
        return Success();
    }

    private async Task<bool> ValidatePatientExists(string patientId)
    {
        // Simulate patient service call
        await Task.Delay(50);
        return true; // Assume patient exists
    }

    private async Task<bool> CheckPatientRestrictions(string patientId, string serviceType)
    {
        // Simulate medical restriction check
        await Task.Delay(30);
        return false; // Assume no restrictions
    }

    private async Task<bool> CheckInsuranceCoverage(string patientId, string serviceType)
    {
        // Simulate insurance check
        await Task.Delay(40);
        return DateTime.UtcNow.Millisecond % 2 == 0; // 50% coverage
    }
}

public class ReserveAppointmentSlotStep : CompensatableSagaStepBase
{
    public override string StepName => "ReserveAppointmentSlot";
    public override int Order => 4;

    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var doctorId = context.GetData<string>("DoctorId");
        var appointmentDate = context.GetData<DateTime>("AppointmentDate");
        var serviceType = context.GetData<string>("ServiceType");

        if (string.IsNullOrEmpty(doctorId))
            return Failure("DoctorId is required");
            
        if (string.IsNullOrEmpty(serviceType))
            return Failure("ServiceType is required");

        await Task.Delay(250, cancellationToken);

        // Reserve the time slot
        var reservationId = Guid.NewGuid().ToString();
        var slotDuration = GetServiceDuration(serviceType);

        // This would call the Schedule Service API
        var reservationSuccess = await ReserveTimeSlot(doctorId, appointmentDate, slotDuration, reservationId);
        
        if (!reservationSuccess)
            return Failure("Failed to reserve the time slot");

        context.SetData("ReservationId", reservationId);
        context.SetData("SlotDuration", slotDuration);
        context.SetData("SlotReserved", true);

        return Success();
    }

    public override async Task<SagaStepResult> CompensateAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var reservationId = context.GetData<string>("ReservationId");

        if (!string.IsNullOrEmpty(reservationId))
        {
            // Release the reserved slot
            await ReleaseTimeSlot(reservationId);
            context.SetData("SlotReleased", true);
        }

        return Success();
    }

    private TimeSpan GetServiceDuration(string serviceType)
    {
        return serviceType.ToLower() switch
        {
            "consultation" => TimeSpan.FromMinutes(30),
            "checkup" => TimeSpan.FromMinutes(45),
            "procedure" => TimeSpan.FromMinutes(60),
            _ => TimeSpan.FromMinutes(30)
        };
    }

    private async Task<bool> ReserveTimeSlot(string doctorId, DateTime appointmentDate, TimeSpan duration, string reservationId)
    {
        // Simulate schedule service call
        await Task.Delay(100);
        return DateTime.UtcNow.Millisecond % 4 != 0; // 75% success rate
    }

    private async Task ReleaseTimeSlot(string reservationId)
    {
        // Simulate schedule service call
        await Task.Delay(50);
    }
}

public class ProcessAppointmentPaymentStep : CompensatableSagaStepBase
{
    public override string StepName => "ProcessAppointmentPayment";
    public override int Order => 5;

    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var patientId = context.GetData<string>("PatientId");
        var serviceType = context.GetData<string>("ServiceType");
        var insuranceCovered = context.GetData<bool>("InsuranceCovered");

        if (string.IsNullOrEmpty(patientId))
            return Failure("PatientId is required");
            
        if (string.IsNullOrEmpty(serviceType))
            return Failure("ServiceType is required");

        await Task.Delay(400, cancellationToken);

        // Calculate payment amount
        var totalAmount = CalculateServiceCost(serviceType);
        var patientAmount = insuranceCovered ? totalAmount * 0.2m : totalAmount; // 20% copay if insured

        if (patientAmount > 0)
        {
            // Process payment
            var paymentResult = await ProcessPayment(patientId, patientAmount);
            
            if (!paymentResult.Success)
            {
                return Failure($"Payment processing failed: {paymentResult.ErrorMessage}", 
                    shouldRetry: true, retryDelay: TimeSpan.FromSeconds(30));
            }

            context.SetData("TransactionId", paymentResult.TransactionId);
            context.SetData("AmountPaid", patientAmount);
        }

        context.SetData("TotalCost", totalAmount);
        context.SetData("PaymentProcessed", true);

        return Success();
    }

    public override async Task<SagaStepResult> CompensateAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var transactionId = context.GetData<string>("TransactionId");

        if (!string.IsNullOrEmpty(transactionId))
        {
            // Refund the payment
            var refundResult = await RefundPayment(transactionId);
            
            if (!refundResult.Success)
            {
                return Failure($"Refund failed: {refundResult.ErrorMessage}", 
                    shouldRetry: true, retryDelay: TimeSpan.FromSeconds(30));
            }

            context.SetData("RefundId", refundResult.RefundId);
            context.SetData("PaymentRefunded", true);
        }

        return Success();
    }

    private decimal CalculateServiceCost(string serviceType)
    {
        return serviceType.ToLower() switch
        {
            "consultation" => 150.00m,
            "checkup" => 200.00m,
            "procedure" => 500.00m,
            _ => 150.00m
        };
    }

    private async Task<PaymentResult> ProcessPayment(string patientId, decimal amount)
    {
        // Simulate payment service call
        await Task.Delay(200);
        
        var success = DateTime.UtcNow.Millisecond % 5 != 0; // 80% success rate
        
        return new PaymentResult
        {
            Success = success,
            TransactionId = success ? Guid.NewGuid().ToString() : null,
            ErrorMessage = success ? null : "Payment declined"
        };
    }

    private async Task<RefundResult> RefundPayment(string transactionId)
    {
        // Simulate refund service call
        await Task.Delay(150);
        
        return new RefundResult
        {
            Success = true,
            RefundId = Guid.NewGuid().ToString()
        };
    }
}

public class CreateAppointmentRecordStep : CompensatableSagaStepBase
{
    public override string StepName => "CreateAppointmentRecord";
    public override int Order => 6;

    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var patientId = context.GetData<string>("PatientId");
        var doctorId = context.GetData<string>("DoctorId");
        var appointmentDate = context.GetData<DateTime>("AppointmentDate");
        var serviceType = context.GetData<string>("ServiceType");
        var reservationId = context.GetData<string>("ReservationId");
        var transactionId = context.GetData<string>("TransactionId");

        if (string.IsNullOrEmpty(patientId))
            return Failure("PatientId is required");
            
        if (string.IsNullOrEmpty(doctorId))
            return Failure("DoctorId is required");
            
        if (string.IsNullOrEmpty(serviceType))
            return Failure("ServiceType is required");

        await Task.Delay(200, cancellationToken);

        // Create the appointment record
        var appointmentId = Guid.NewGuid().ToString();
        
        var appointment = new AppointmentRecord
        {
            AppointmentId = appointmentId,
            PatientId = patientId,
            DoctorId = doctorId,
            AppointmentDate = appointmentDate,
            ServiceType = serviceType,
            Status = "Confirmed",
            ReservationId = reservationId ?? string.Empty,
            TransactionId = transactionId ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        // Save to database
        var saveResult = await SaveAppointmentRecord(appointment);
        
        if (!saveResult)
            return Failure("Failed to create appointment record");

        context.SetData("AppointmentId", appointmentId);
        context.SetData("AppointmentCreated", true);

        return Success();
    }

    public override async Task<SagaStepResult> CompensateAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var appointmentId = context.GetData<string>("AppointmentId");

        if (!string.IsNullOrEmpty(appointmentId))
        {
            // Cancel/Delete the appointment record
            await CancelAppointmentRecord(appointmentId);
            context.SetData("AppointmentCancelled", true);
        }

        return Success();
    }

    private async Task<bool> SaveAppointmentRecord(AppointmentRecord appointment)
    {
        // Simulate database save
        await Task.Delay(100);
        return true; // Assume success
    }

    private async Task CancelAppointmentRecord(string appointmentId)
    {
        // Simulate database update
        await Task.Delay(50);
    }
}

public class SendAppointmentConfirmationStep : ExecutableSagaStepBase
{
    public override string StepName => "SendAppointmentConfirmation";
    public override int Order => 7;

    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var patientId = context.GetData<string>("PatientId");
        var appointmentId = context.GetData<string>("AppointmentId");
        var appointmentDate = context.GetData<DateTime>("AppointmentDate");

        if (string.IsNullOrEmpty(patientId))
            return Failure("PatientId is required");
            
        if (string.IsNullOrEmpty(appointmentId))
            return Failure("AppointmentId is required");

        await Task.Delay(100, cancellationToken);

        // Send confirmation email/SMS
        await SendConfirmationEmail(patientId, appointmentId, appointmentDate);
        await SendConfirmationSMS(patientId, appointmentId, appointmentDate);

        context.SetData("ConfirmationSent", true);

        return Success();
    }

    private async Task SendConfirmationEmail(string patientId, string appointmentId, DateTime appointmentDate)
    {
        // Simulate notification service call
        await Task.Delay(50);
    }

    private async Task SendConfirmationSMS(string patientId, string appointmentId, DateTime appointmentDate)
    {
        // Simulate notification service call
        await Task.Delay(30);
    }
}

public class UpdateDoctorScheduleStep : ExecutableSagaStepBase
{
    public override string StepName => "UpdateDoctorSchedule";
    public override int Order => 8;

    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var doctorId = context.GetData<string>("DoctorId");
        var appointmentId = context.GetData<string>("AppointmentId");
        var appointmentDate = context.GetData<DateTime>("AppointmentDate");

        if (string.IsNullOrEmpty(doctorId))
            return Failure("DoctorId is required");
            
        if (string.IsNullOrEmpty(appointmentId))
            return Failure("AppointmentId is required");

        await Task.Delay(100, cancellationToken);

        // Update doctor's schedule with the confirmed appointment
        await UpdateDoctorSchedule(doctorId, appointmentId, appointmentDate);

        context.SetData("DoctorScheduleUpdated", true);

        return Success();
    }

    private async Task UpdateDoctorSchedule(string doctorId, string appointmentId, DateTime appointmentDate)
    {
        // Simulate doctor service call
        await Task.Delay(50);
    }
}

public class CreateCalendarEventStep : ExecutableSagaStepBase
{
    public override string StepName => "CreateCalendarEvent";
    public override int Order => 9;

    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var patientId = context.GetData<string>("PatientId");
        var doctorId = context.GetData<string>("DoctorId");
        var appointmentId = context.GetData<string>("AppointmentId");
        var appointmentDate = context.GetData<DateTime>("AppointmentDate");

        if (string.IsNullOrEmpty(patientId))
            return Failure("PatientId is required");
            
        if (string.IsNullOrEmpty(doctorId))
            return Failure("DoctorId is required");
            
        if (string.IsNullOrEmpty(appointmentId))
            return Failure("AppointmentId is required");

        await Task.Delay(150, cancellationToken);

        // Create calendar events for both patient and doctor
        await CreatePatientCalendarEvent(patientId, appointmentId, appointmentDate);
        await CreateDoctorCalendarEvent(doctorId, appointmentId, appointmentDate);

        context.SetData("CalendarEventsCreated", true);

        return Success();
    }

    private async Task CreatePatientCalendarEvent(string patientId, string appointmentId, DateTime appointmentDate)
    {
        // Simulate calendar service call
        await Task.Delay(50);
    }

    private async Task CreateDoctorCalendarEvent(string doctorId, string appointmentId, DateTime appointmentDate)
    {
        // Simulate calendar service call
        await Task.Delay(50);
    }
}

public class NotifyRelatedPartiesStep : ExecutableSagaStepBase
{
    public override string StepName => "NotifyRelatedParties";
    public override int Order => 10;

    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        var doctorId = context.GetData<string>("DoctorId");
        var appointmentId = context.GetData<string>("AppointmentId");
        var appointmentDate = context.GetData<DateTime>("AppointmentDate");

        if (string.IsNullOrEmpty(doctorId))
            return Failure("DoctorId is required");
            
        if (string.IsNullOrEmpty(appointmentId))
            return Failure("AppointmentId is required");

        await Task.Delay(100, cancellationToken);

        // Notify related parties (clinic staff, assistants, etc.)
        await NotifyClinicStaff(doctorId, appointmentId, appointmentDate);
        await NotifyDoctorAssistants(doctorId, appointmentId, appointmentDate);

        context.SetData("RelatedPartiesNotified", true);

        return Success();
    }

    private async Task NotifyClinicStaff(string doctorId, string appointmentId, DateTime appointmentDate)
    {
        // Simulate notification service call
        await Task.Delay(30);
    }

    private async Task NotifyDoctorAssistants(string doctorId, string appointmentId, DateTime appointmentDate)
    {
        // Simulate notification service call
        await Task.Delay(30);
    }
}

// Supporting classes
public class AppointmentRecord
{
    public string AppointmentId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public string ServiceType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ReservationId { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class PaymentResult
{
    public bool Success { get; set; }
    public string? TransactionId { get; set; }
    public string? ErrorMessage { get; set; }
}

public class RefundResult
{
    public bool Success { get; set; }
    public string? RefundId { get; set; }
    public string? ErrorMessage { get; set; }
}

// Integration Events
public class AppointmentBookingInitiatedEvent : IntegrationEvent
{
    public string PatientId { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public string ServiceType { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
}

public class AppointmentBookingConfirmedEvent : IntegrationEvent
{
    public string AppointmentId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public string ServiceType { get; set; } = string.Empty;
    public decimal TotalCost { get; set; }
    public string TransactionId { get; set; } = string.Empty;
}

public class AppointmentBookingFailedEvent : IntegrationEvent
{
    public string PatientId { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public string ServiceType { get; set; } = string.Empty;
    public string FailureReason { get; set; } = string.Empty;
    public string FailedStep { get; set; } = string.Empty;
}
