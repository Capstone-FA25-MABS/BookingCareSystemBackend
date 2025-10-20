using BookingCare.Shared.Common.Interfaces;
using BookingCare.Shared.Common.Extensions;
using BookingCare.Shared.EventBus.Events;

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

/// <summary>
/// ?? Enhanced User Profile Updated Event with detailed info for cache invalidation
/// </summary>
public class UserProfileUpdatedEvent : IntegrationEvent
{
    /// <summary>
    /// User ID (primary identifier)
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Account ID (for cache key lookups)
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Email address (for cache invalidation by email)
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Previous email (if changed, for old cache key cleanup)
    /// </summary>
    public string? PreviousEmail { get; set; }

    /// <summary>
    /// Full name for cache updates
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// First name
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Last name
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Avatar URL for cache updates
    /// </summary>
    public string AvatarUrl { get; set; } = string.Empty;

    /// <summary>
    /// Phone number
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// User role (PATIENT, DOCTOR, etc.) for cache updates
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Gender
    /// </summary>
    public string? Gender { get; set; }

    /// <summary>
    /// Date of birth
    /// </summary>
    public DateTime? DateOfBirth { get; set; }

    /// <summary>
    /// Address
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Correlation ID for tracking
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// List of fields that were updated (for selective cache invalidation)
    /// </summary>
    public List<string> UpdatedFields { get; set; } = new();
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

public class AppointmentCancelledIntegrationEvent : IntegrationEvent
{
    /// <summary>
    /// ID of the cancelled appointment
    /// </summary>
    public Guid AppointmentId { get; set; }

    /// <summary>
    /// ID of the patient
    /// </summary>
    public Guid PatientId { get; set; }

    /// <summary>
    /// ID of the doctor (if assigned)
    /// </summary>
    public Guid? DoctorId { get; set; }

    /// <summary>
    /// ID of the hospital
    /// </summary>
    public Guid? HospitalId { get; set; }

    /// <summary>
    /// Appointment date
    /// </summary>
    public DateTime AppointmentDate { get; set; }

    /// <summary>
    /// Appointment type (enum value as int to avoid coupling)
    /// </summary>
    public int AppointmentType { get; set; }

    /// <summary>
    /// Cancellation reason
    /// </summary>
    public string CancellationReason { get; set; } = string.Empty;

    /// <summary>
    /// ID of the staff who cancelled (if applicable)
    /// </summary>
    public Guid? CancelledByStaffId { get; set; }

    public Guid? CancelledByPatientId { get; set; } // New field for patient cancellation

    /// <summary>
    /// Cancellation timestamp
    /// </summary>
    public DateTime CancelledAt { get; set; }

    /// <summary>
    /// Payment ID associated with this appointment (if any)
    /// Will be populated from Payment Service
    /// </summary>
    public Guid? PaymentId { get; set; }

    /// <summary>
    /// Refund percentage based on cancellation policy (0-100)
    /// 100 = full refund, 50 = half refund, 0 = no refund
    /// </summary>
    public decimal RefundPercentage { get; set; } = 100m;
}


public class AppointmentRefundRequestedIntegrationEvent : IntegrationEvent
{
    /// <summary>
    /// ID of the refund history record
    /// </summary>
    public Guid RefundHistoryId { get; set; }

    /// <summary>
    /// ID of the appointment that was cancelled
    /// </summary>
    public Guid AppointmentId { get; set; }

    /// <summary>
    /// ID of the patient requesting refund
    /// </summary>
    public Guid PatientId { get; set; }

    /// <summary>
    /// ID of the hospital processing refund
    /// </summary>
    public Guid HospitalId { get; set; }

    /// <summary>
    /// ID of the payment being refunded
    /// </summary>
    public Guid PaymentId { get; set; }

    /// <summary>
    /// Amount to be refunded
    /// </summary>
    public decimal RefundAmount { get; set; }

    public decimal OriginalAmount { get; set; } // New field - original payment amount
    public decimal RefundPercentage { get; set; } // New field - refund percentage (0-100)

    /// <summary>
    /// Reason for cancellation/refund
    /// </summary>
    public string CancellationReason { get; set; } = string.Empty;

    /// <summary>
    /// Whether patient has bank account on file
    /// </summary>
    public bool HasBankAccount { get; set; }

    /// <summary>
    /// Bank account ID if available
    /// </summary>
    public Guid? BankAccountId { get; set; }

    /// <summary>
    /// Initial refund status (WAITING or PENDING)
    /// </summary>
    public string RefundStatus { get; set; } = string.Empty;

    /// <summary>
    /// Patient email (for notification)
    /// </summary>
    public string? PatientEmail { get; set; }

    /// <summary>
    /// Patient phone (for notification)
    /// </summary>
    public string? PatientPhone { get; set; }

    /// <summary>
    /// Patient full name
    /// </summary>
    public string? PatientFullName { get; set; }

    /// <summary>
    /// Hospital name
    /// </summary>
    public string? HospitalName { get; set; }

    /// <summary>
    /// Appointment date (for reference in notification)
    /// </summary>
    public DateTime AppointmentDate { get; set; }
    public bool IsPartialRefund => RefundPercentage > 0 && RefundPercentage < 100; // Helper property
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

public class BankAccountCreatedIntegrationEvent : IntegrationEvent
{
    /// <summary>
    /// ID of the bank account that was created/updated
    /// </summary>
    public Guid BankAccountId { get; set; }

    /// <summary>
    /// ID of the user who owns the bank account
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Bank code (e.g., VCB, TCB)
    /// </summary>
    public string BankCode { get; set; } = string.Empty;

    /// <summary>
    /// Bank name
    /// </summary>
    public string BankName { get; set; } = string.Empty;

    /// <summary>
    /// Bank account number
    /// </summary>
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// Account holder name
    /// </summary>
    public string AccountName { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is the default account
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Whether this is a new account or update
    /// </summary>
    public bool IsNewAccount { get; set; }
}

/// <summary>
/// Event published when a refund is completed and transferred to patient's bank account
/// This event is consumed by Notification Service to send success notifications to the patient
/// </summary>
public class RefundHistoryCompletedIntegrationEvent : IntegrationEvent
{
    /// <summary>
    /// ID of the refund history
    /// </summary>
    public Guid RefundHistoryId { get; set; }

    /// <summary>
    /// ID of the refunded payment
    /// </summary>
    public Guid PaymentId { get; set; }

    /// <summary>
    /// ID of the user receiving the refund
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// User email (for notification)
    /// </summary>
    public string? UserEmail { get; set; }

    /// <summary>
    /// User phone (for notification)
    /// </summary>
    public string? UserPhone { get; set; }

    /// <summary>
    /// User full name
    /// </summary>
    public string? UserFullName { get; set; }

    /// <summary>
    /// Amount refunded
    /// </summary>
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// ID of the receiving bank account
    /// </summary>
    public Guid BankAccountId { get; set; }

    /// <summary>
    /// Bank account information
    /// </summary>
    public BankAccountInfo BankAccount { get; set; } = new();

    /// <summary>
    /// Transfer date
    /// </summary>
    public DateTime TransferDate { get; set; }

    /// <summary>
    /// ID of the staff who processed the refund
    /// </summary>
    public Guid? ProcessedByStaffId { get; set; }

    /// <summary>
    /// Notes from staff
    /// </summary>
    public string? StaffNotes { get; set; }

    /// <summary>
    /// Completion time
    /// </summary>
    public DateTime CompletedAt { get; set; }
}

/// <summary>
/// Event published when a bank account issue is reported for a refund
/// This event is consumed by Notification Service to send notifications to the patient about incorrect bank account information
/// </summary>
public class RefundHistoryBankIssueReportedIntegrationEvent : IntegrationEvent
{
    /// <summary>
    /// ID of the refund history
    /// </summary>
    public Guid RefundHistoryId { get; set; }

    /// <summary>
    /// ID of the user to notify
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// User email (if available)
    /// </summary>
    public string? UserEmail { get; set; }

    /// <summary>
    /// User phone (if available)
    /// </summary>
    public string? UserPhone { get; set; }

    /// <summary>
    /// Refund amount
    /// </summary>
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// Current bank account information that has issue
    /// </summary>
    public BankAccountInfo? BankAccount { get; set; }

    /// <summary>
    /// Description of the issue
    /// </summary>
    public string IssueDescription { get; set; } = string.Empty;

    /// <summary>
    /// Reported time
    /// </summary>
    public DateTime ReportedAt { get; set; }
}

/// <summary>
/// Integration event for appointment cancellation with no refund (0% refund due to late cancellation)
/// This event is published directly to Notification Service to send notification only
/// </summary>
public class AppointmentNoRefundNotificationEvent : IntegrationEvent
{
    public Guid AppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string CancellationReason { get; set; } = string.Empty;
    public DateTime CancelledAt { get; set; }

    // Patient information for notification
    public string? PatientEmail { get; set; }
    public string? PatientPhone { get; set; }
    public string? PatientFullName { get; set; }
}

/// <summary>
/// Bank account information included in refund events
/// </summary>
public class BankAccountInfo
{
    public string BankCode { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty; // masked
    public string AccountName { get; set; } = string.Empty;
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

/// <summary>
/// Event published when an appointment should be deleted due to payment failure
/// This event is consumed by Appointment Service to remove the appointment
/// </summary>
public class AppointmentDeleteRequestedIntegrationEvent : IntegrationEvent
{
    /// <summary>
    /// ID of the appointment to be deleted
    /// </summary>
    public Guid AppointmentId { get; set; }

    /// <summary>
    /// ID of the patient who created the appointment
    /// </summary>
    public Guid PatientId { get; set; }

    /// <summary>
    /// ID of the doctor (if assigned)
    /// </summary>
    public Guid? DoctorId { get; set; }

    /// <summary>
    /// ID of the hospital where appointment was scheduled
    /// </summary>
    public Guid? HospitalId { get; set; }

    /// <summary>
    /// Appointment date and time
    /// </summary>
    public DateTime AppointmentDate { get; set; }

    /// <summary>
    /// Appointment type (enum value as int to avoid coupling)
    /// </summary>
    public int AppointmentType { get; set; }

    /// <summary>
    /// Reason for deletion (payment failure reason)
    /// </summary>
    public string DeletionReason { get; set; } = string.Empty;

    /// <summary>
    /// ID of the failed payment that triggered this deletion
    /// </summary>
    public Guid? PaymentId { get; set; }

    /// <summary>
    /// Payment method that failed (VNPay, PayOS, etc.)
    /// </summary>
    public string? PaymentMethod { get; set; }

    /// <summary>
    /// Payment failure code/message
    /// </summary>
    public string? PaymentFailureReason { get; set; }

    /// <summary>
    /// When the deletion was requested
    /// </summary>
    public DateTime RequestedAt { get; set; }

    /// <summary>
    /// Correlation ID for tracking
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;
}

/// <summary>
/// Event published when an appointment payment is successful
/// This event is consumed by Appointment Service to send booking success notifications to the patient
/// </summary>
public class AppointmentPaymentSuccessIntegrationEvent : IntegrationEvent
{
    /// <summary>
    /// ID of the appointment that was successfully paid for
    /// </summary>
    public Guid AppointmentId { get; set; }

    /// <summary>
    /// ID of the patient who made the payment
    /// </summary>
    public Guid PatientId { get; set; }

    /// <summary>
    /// ID of the successful payment
    /// </summary>
    public Guid PaymentId { get; set; }

    /// <summary>
    /// Amount that was paid
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Payment method used (VNPay, PayOS, etc.)
    /// </summary>
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>
    /// Transaction ID from payment gateway
    /// </summary>
    public string? TransactionId { get; set; }

    /// <summary>
    /// When the payment was completed
    /// </summary>
    public DateTime PaymentCompletedAt { get; set; }

    /// <summary>
    /// Correlation ID for tracking
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;
}

/// <summary>
/// Event published when appointment booking is successful and payment is completed
/// This event is consumed by Notification Service to send booking success email to patient
/// 
/// Note: AppointmentBookingEmailData DTO in Notification Service maps directly from this event
/// to avoid duplication of properties. See AppointmentBookingEmailData.FromEvent() method.
/// 
/// Uses pure composition pattern - NO delegation properties to eliminate SonarQube "Duplicated Lines" issue.
/// </summary>
public class AppointmentBookingSuccessNotificationEvent : IntegrationEvent
{
    /// <summary>
    /// ID of the appointment that was successfully booked
    /// </summary>
    public Guid AppointmentId { get; set; }

    /// <summary>
    /// ID of the patient who booked the appointment
    /// </summary>
    public Guid PatientId { get; set; }

    /// <summary>
    /// Patient email address
    /// </summary>
    public string PatientEmail { get; set; } = string.Empty;

    /// <summary>
    /// Appointment data - contains all appointment-related information
    /// Access properties via: AppointmentData.PatientName, AppointmentData.AppointmentDate, etc.
    /// NO delegation properties to eliminate code duplication!
    /// </summary>
    public AppointmentData AppointmentData { get; set; } = new();

    /// <summary>
    /// Email subject line
    /// </summary>
    public string EmailSubject { get; set; } = string.Empty;

    /// <summary>
    /// Correlation ID for tracking
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    // NO delegation properties here - completely eliminates duplicate code!
    // Access appointment data via: event.AppointmentData.PatientName, etc.
}
