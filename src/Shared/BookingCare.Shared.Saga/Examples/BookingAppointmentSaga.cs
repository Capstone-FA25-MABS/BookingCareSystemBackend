using BookingCare.Shared.EventBus.Events;
using BookingCare.Shared.Saga.Abstractions;
using BookingCare.Shared.Saga.Core;
using BookingCare.Shared.Saga.Models;

namespace BookingCare.Shared.Saga.Examples;

/// <summary>
/// Saga for booking appointment process
/// Coordinates: User Service, Doctor Service, Schedule Service, Payment Service, Notification Service
/// </summary>
public class BookingAppointmentSaga : SagaDefinitionBase
{
    public override string SagaName => "BookingAppointment";
    public override TimeSpan GlobalTimeout => TimeSpan.FromMinutes(15);

    public BookingAppointmentSaga()
    {
        AddStep<ValidateUserStep>();
        AddStep<CheckDoctorAvailabilityStep>();
        AddStep<ReserveTimeSlotStep>();
        AddStep<ProcessPaymentStep>();
        AddStep<CreateAppointmentStep>();
        AddStep<SendConfirmationNotificationStep>();
    }
}

// Example saga steps for booking appointment
public class ValidateUserStep : CompensatableSagaStepBase
{
    public override string StepName => "ValidateUser";
    public override int Order => 1;

    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        // Call User Service to validate user
        var userId = context.GetData<string>("UserId");

        // Simulate user validation
        await Task.Delay(100, cancellationToken);

        if (string.IsNullOrEmpty(userId))
        {
            return Failure("User ID is required");
        }

        // Store validation result
        context.SetData("UserValidated", true);
        context.SetData("UserEmail", "user@example.com");

        return Success();
    }

    public override async Task<SagaStepResult> CompensateAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        // No compensation needed for user validation
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
        // Call Doctor Service to check availability
        var doctorId = context.GetData<string>("DoctorId");
        var appointmentDate = context.GetData<DateTime>("AppointmentDate");

        await Task.Delay(200, cancellationToken);

        if (string.IsNullOrEmpty(doctorId))
        {
            return Failure("Doctor ID is required");
        }

        // Simulate availability check
        var isAvailable = DateTime.UtcNow.Millisecond % 2 == 0; // Random availability

        if (!isAvailable)
        {
            return Failure("Doctor is not available for the requested time");
        }

        context.SetData("DoctorAvailable", true);
        return Success();
    }

    public override async Task<SagaStepResult> CompensateAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        // No compensation needed for availability check
        await Task.Delay(1, cancellationToken);
        return Success();
    }
}

public class ReserveTimeSlotStep : CompensatableSagaStepBase
{
    public override string StepName => "ReserveTimeSlot";
    public override int Order => 3;

    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        // Call Schedule Service to reserve time slot
        var doctorId = context.GetData<string>("DoctorId");
        var appointmentDate = context.GetData<DateTime>("AppointmentDate");

        await Task.Delay(300, cancellationToken);

        // Simulate reservation
        var reservationId = Guid.NewGuid();
        context.SetData("ReservationId", reservationId.ToString());
        context.SetData("TimeSlotReserved", true);

        return Success();
    }

    public override async Task<SagaStepResult> CompensateAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        // Cancel the reservation
        var reservationId = context.GetData<string>("ReservationId");

        if (!string.IsNullOrEmpty(reservationId))
        {
            // Call Schedule Service to cancel reservation
            await Task.Delay(100, cancellationToken);
            context.SetData("TimeSlotReserved", false);
        }

        return Success();
    }
}

public class ProcessPaymentStep : CompensatableSagaStepBase
{
    public override string StepName => "ProcessPayment";
    public override int Order => 4;

    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        // Call Payment Service to process payment
        var amount = context.GetData<decimal>("Amount");
        var userId = context.GetData<string>("UserId");

        await Task.Delay(500, cancellationToken);

        if (amount <= 0)
        {
            return Failure("Invalid payment amount");
        }

        // Simulate payment processing
        var paymentSuccessful = DateTime.UtcNow.Millisecond % 3 != 0; // Random payment success

        if (!paymentSuccessful)
        {
            return Failure("Payment processing failed", shouldRetry: true, retryDelay: TimeSpan.FromSeconds(30));
        }

        var transactionId = Guid.NewGuid();
        context.SetData("TransactionId", transactionId.ToString());
        context.SetData("PaymentProcessed", true);

        return Success();
    }

    public override async Task<SagaStepResult> CompensateAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        // Refund the payment
        var transactionId = context.GetData<string>("TransactionId");

        if (!string.IsNullOrEmpty(transactionId))
        {
            // Call Payment Service to refund
            await Task.Delay(300, cancellationToken);
            context.SetData("PaymentRefunded", true);
        }

        return Success();
    }
}

public class CreateAppointmentStep : CompensatableSagaStepBase
{
    public override string StepName => "CreateAppointment";
    public override int Order => 5;

    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        // Call Appointment Service to create appointment
        var userId = context.GetData<string>("UserId");
        var doctorId = context.GetData<string>("DoctorId");
        var appointmentDate = context.GetData<DateTime>("AppointmentDate");
        var transactionId = context.GetData<string>("TransactionId");

        await Task.Delay(200, cancellationToken);

        // Create appointment
        var appointmentId = Guid.NewGuid();
        context.SetData("AppointmentId", appointmentId.ToString());
        context.SetData("AppointmentCreated", true);

        return Success();
    }

    public override async Task<SagaStepResult> CompensateAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        // Cancel the appointment
        var appointmentId = context.GetData<string>("AppointmentId");

        if (!string.IsNullOrEmpty(appointmentId))
        {
            // Call Appointment Service to cancel
            await Task.Delay(100, cancellationToken);
            context.SetData("AppointmentCancelled", true);
        }

        return Success();
    }
}

public class SendConfirmationNotificationStep : ExecutableSagaStepBase
{
    public override string StepName => "SendConfirmationNotification";
    public override int Order => 6;

    public override async Task<SagaStepResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        // Call Notification Service to send confirmation
        var userEmail = context.GetData<string>("UserEmail");
        var appointmentId = context.GetData<string>("AppointmentId");

        await Task.Delay(100, cancellationToken);

        // Send notification (non-compensatable step)
        context.SetData("ConfirmationSent", true);

        return Success();
    }
}

// Example integration events for appointment booking
public class AppointmentBookingRequestedEvent : IntegrationEvent
{
    public string UserId { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public decimal Amount { get; set; }
    public string ServiceType { get; set; } = string.Empty;
}

public class AppointmentBookingCompletedEvent : IntegrationEvent
{
    public string AppointmentId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public string TransactionId { get; set; } = string.Empty;
}

public class AppointmentBookingFailedEvent : IntegrationEvent
{
    public string UserId { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string FailedStep { get; set; } = string.Empty;
}

// Example saga event handler
public class AppointmentBookingSagaEventHandler : ISagaEventHandler<AppointmentBookingRequestedEvent>
{
    private readonly ISagaManager _sagaManager;

    public AppointmentBookingSagaEventHandler(ISagaManager sagaManager)
    {
        _sagaManager = sagaManager;
    }

    public async Task HandleAsync(AppointmentBookingRequestedEvent @event, SagaContext context, CancellationToken cancellationToken = default)
    {
        // Prepare saga context with event data
        context.SetData("UserId", @event.UserId);
        context.SetData("DoctorId", @event.DoctorId);
        context.SetData("AppointmentDate", @event.AppointmentDate);
        context.SetData("Amount", @event.Amount);
        context.SetData("ServiceType", @event.ServiceType);
        context.CorrelationId = @event.Id.ToString();

        // Start the booking saga
        await _sagaManager.StartSagaAsync<BookingAppointmentSaga>(context, cancellationToken);
    }
}
