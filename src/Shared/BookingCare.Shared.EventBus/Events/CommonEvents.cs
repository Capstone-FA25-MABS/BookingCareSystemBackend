namespace BookingCare.Shared.EventBus.Events;

// User-related events
public class UserRegisteredEvent : IntegrationEvent
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
}

public class UserProfileUpdatedEvent : IntegrationEvent
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}

public class UserEmailPhoneSyncRequestedEvent : IntegrationEvent
{
    public Guid AccountId { get; set; }
    public Guid UserId { get; set; }
    public string? OriginalEmail { get; set; }
    public string? OriginalPhone { get; set; }
    public string? NewEmail { get; set; }
    public string? NewPhone { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
}

public class UserEmailPhoneSyncCompletedEvent : IntegrationEvent
{
    public Guid AccountId { get; set; }
    public Guid UserId { get; set; }
    public string? UpdatedEmail { get; set; }
    public string? UpdatedPhone { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CompletedAt { get; set; }
}

public class UserEmailPhoneSyncFailedEvent : IntegrationEvent
{
    public Guid AccountId { get; set; }
    public Guid UserId { get; set; }
    public string? OriginalEmail { get; set; }
    public string? OriginalPhone { get; set; }
    public string? NewEmail { get; set; }
    public string? NewPhone { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime FailedAt { get; set; }
}

public class UserDeletedEvent : IntegrationEvent
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime DeletedAt { get; set; }
}

// Doctor-related events
public class DoctorRegisteredEvent : IntegrationEvent
{
    public Guid DoctorId { get; set; }
    public Guid UserId { get; set; }
    public string SpecializationId { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
}

public class DoctorProfileUpdatedEvent : IntegrationEvent
{
    public Guid DoctorId { get; set; }
    public string SpecializationId { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Appointment-related events
public class AppointmentCreatedEvent : IntegrationEvent
{
    public Guid AppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid? ClinicId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AppointmentUpdatedEvent : IntegrationEvent
{
    public Guid AppointmentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? AppointmentDate { get; set; }
    public string? Notes { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class AppointmentCancelledEvent : IntegrationEvent
{
    public Guid AppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public string CancellationReason { get; set; } = string.Empty;
    public DateTime CancelledAt { get; set; }
    public decimal RefundAmount { get; set; }
}

public class AppointmentCompletedEvent : IntegrationEvent
{
    public Guid AppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public string? Diagnosis { get; set; }
    public string? Treatment { get; set; }
    public DateTime CompletedAt { get; set; }
}

// Payment-related events
public class PaymentProcessedEvent : IntegrationEvent
{
    public Guid PaymentId { get; set; }
    public Guid AppointmentId { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? TransactionId { get; set; }
    public DateTime ProcessedAt { get; set; }
}

public class PaymentFailedEvent : IntegrationEvent
{
    public Guid PaymentId { get; set; }
    public Guid AppointmentId { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime FailedAt { get; set; }
}

public class RefundProcessedEvent : IntegrationEvent
{
    public Guid RefundId { get; set; }
    public Guid OriginalPaymentId { get; set; }
    public Guid AppointmentId { get; set; }
    public Guid UserId { get; set; }
    public decimal RefundAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
}

// Notification-related events
public class NotificationSendEvent : IntegrationEvent
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Email, SMS, Push
    public Dictionary<string, object> Data { get; set; } = new();
    public DateTime ScheduledAt { get; set; }
}

// Review-related events
public class ReviewCreatedEvent : IntegrationEvent
{
    public Guid ReviewId { get; set; }
    public Guid AppointmentId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid PatientId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Analytics events
public class UserActivityEvent : IntegrationEvent
{
    public Guid UserId { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public Dictionary<string, object> Properties { get; set; } = new();
    public DateTime Timestamp { get; set; }
}
