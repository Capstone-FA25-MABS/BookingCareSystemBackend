using BookingCare.Shared.Common.Interfaces;
using BookingCare.Shared.Common.Extensions;
using BookingCare.Shared.Common.Enums;
using BookingCare.Shared.Common.Models;
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
/// Used by both User Service and Doctor Service (with Role = "DOCTOR")
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

/// <summary>
/// Event published when a doctor account is created with auto-generated password
/// This event is consumed by Notification Service to send login credentials via email
/// </summary>
public class DoctorCredentialsGeneratedEvent : IntegrationEvent
{
    /// <summary>
    /// Doctor's email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Doctor's full name
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Auto-generated password
    /// </summary>
    public string GeneratedPassword { get; set; } = string.Empty;

    /// <summary>
    /// Hospital ID
    /// </summary>
    public Guid? HospitalId { get; set; }

    /// <summary>
    /// Login URL for the system
    /// </summary>
    public string LoginUrl { get; set; } = string.Empty;
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

    // Patient information for notification
    public string? PatientEmail { get; set; }
    public string? PatientPhone { get; set; }
    public string? PatientFullName { get; set; }

    // Reschedule options (for staff cancellation only)
    public string? RescheduleToken { get; set; }
    public DateTime? RescheduleTokenExpiry { get; set; }
    public string? SameDoctorRescheduleUrl { get; set; }
    public string? ChooseNewDoctorUrl { get; set; }
    public string? RefundRequestUrl { get; set; }

    // Doctor change refund context (Option 3: Choose new doctor with lower price)
    /// <summary>
    /// Cancellation source - helps identify the context for notifications
    /// Values: "PATIENT_CANCELLED", "STAFF_CANCELLED", "DOCTOR_CHANGE_REFUND"
    /// </summary>
    public string? CancellationSource { get; set; }

    /// <summary>
    /// Original doctor ID (when changing to new doctor)
    /// </summary>
    public Guid? OriginalDoctorId { get; set; }

    /// <summary>
    /// Original doctor name (for refund history transparency)
    /// </summary>
    public string? OriginalDoctorName { get; set; }

    /// <summary>
    /// Original consultation fee (deposit paid)
    /// </summary>
    public decimal? OriginalConsultationFee { get; set; }

    /// <summary>
    /// New doctor ID (when changing to new doctor)
    /// </summary>
    public Guid? NewDoctorId { get; set; }

    /// <summary>
    /// New doctor name (for refund history transparency)
    /// </summary>
    public string? NewDoctorName { get; set; }

    /// <summary>
    /// New consultation fee (new deposit amount)
    /// </summary>
    public decimal? NewConsultationFee { get; set; }

    /// <summary>
    /// Refund amount (difference when new doctor is cheaper)
    /// </summary>
    public decimal? RefundAmount { get; set; }
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

    // Doctor change refund context (Option 3: Choose new doctor with lower price)
    /// <summary>
    /// Cancellation source - helps Notification Service create appropriate templates
    /// Values: "PATIENT_CANCELLED", "STAFF_CANCELLED", "DOCTOR_CHANGE_REFUND"
    /// </summary>
    public string? CancellationSource { get; set; }

    /// <summary>
    /// Original doctor name (for doctor change refund notification)
    /// </summary>
    public string? OriginalDoctorName { get; set; }

    /// <summary>
    /// New doctor name (for doctor change refund notification)
    /// </summary>
    public string? NewDoctorName { get; set; }

    /// <summary>
    /// Original consultation fee (for doctor change refund notification)
    /// </summary>
    public decimal? OriginalConsultationFee { get; set; }

    /// <summary>
    /// New consultation fee (for doctor change refund notification)
    /// </summary>
    public decimal? NewConsultationFee { get; set; }
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
/// Base class for appointment notification events to reduce code duplication
/// Contains common properties shared across different cancellation notification types
/// </summary>
public abstract class AppointmentNotificationEventBase : IntegrationEvent
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

    // Doctor and Hospital info (for context in notification)
    public string? DoctorName { get; set; }
    public string? HospitalName { get; set; }
}

/// <summary>
/// Integration event for successful appointment cancellation without payment
/// Used when appointment is cancelled (eligible for refund) but no payment record exists
/// This event is published directly to Notification Service to send simple cancellation confirmation
/// </summary>
public class AppointmentCancelledSuccessNotificationEvent : AppointmentNotificationEventBase
{
    // All common properties inherited from base class
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
    /// Account ID of the patient (from JWT token) - used for notification system
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

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
    /// Account ID of the patient (from JWT token) - used for notification system
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

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

/// <summary>
/// Event published when appointment is cancelled by staff with reschedule options
/// This is a NOTIFICATION-ONLY event consumed by Notification Service
/// Does NOT trigger refund - patient must explicitly choose refund option
/// </summary>
public class AppointmentCancelledWithOptionsNotificationEvent : AppointmentNotificationEventBase
{
    // Common properties inherited from base class

    // Reschedule options with deep links (4 options)
    public string? RescheduleToken { get; set; }
    public DateTime? RescheduleTokenExpiry { get; set; }

    /// <summary>Option 1: Reschedule with same doctor</summary>
    public string? SameDoctorRescheduleUrl { get; set; }

    /// <summary>Option 2: Confirm new doctor assigned by staff</summary>
    public string? ConfirmNewDoctorUrl { get; set; }

    /// <summary>Option 3: Choose new doctor yourself</summary>
    public string? ChooseNewDoctorUrl { get; set; }

    /// <summary>Option 4: Request refund</summary>
    public string? RefundRequestUrl { get; set; }

    // Potential refund info (for display only, not for processing)
    public decimal? PotentialRefundPercentage { get; set; }
    public decimal? PotentialRefundAmount { get; set; }
}

// Hospital Subscription-related events

/// <summary>
/// Event published when a hospital successfully subscribes to a subscription plan
/// This event is consumed by Notification Service to send confirmation email to hospital
/// </summary>
public class HospitalSubscriptionCreatedEvent : IntegrationEvent
{
    /// <summary>
    /// ID of the hospital subscription
    /// </summary>
    public Guid HospitalSubscriptionId { get; set; }

    /// <summary>
    /// ID of the hospital
    /// </summary>
    public Guid HospitalId { get; set; }

    /// <summary>
    /// Hospital name
    /// </summary>
    public string HospitalName { get; set; } = string.Empty;

    /// <summary>
    /// Hospital email for notification
    /// </summary>
    public string HospitalEmail { get; set; } = string.Empty;

    /// <summary>
    /// Contact person name
    /// </summary>
    public string ContactPersonName { get; set; } = string.Empty;

    /// <summary>
    /// ID of the subscription plan
    /// </summary>
    public Guid SubscriptionPlanId { get; set; }

    /// <summary>
    /// Subscription plan name (e.g., "Gói cơ bản", "Gói nâng cao")
    /// </summary>
    public string PlanName { get; set; } = string.Empty;

    /// <summary>
    /// Billing cycle (MONTHLY, QUARTERLY, YEARLY)
    /// </summary>
    public string BillingCycle { get; set; } = string.Empty;

    /// <summary>
    /// Subscription price
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Start date of subscription
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date of subscription
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Maximum number of doctors allowed
    /// </summary>
    public int? MaxDoctors { get; set; }

    /// <summary>
    /// Maximum number of appointments allowed per month
    /// </summary>
    public int? MaxAppointmentsPerMonth { get; set; }

    /// <summary>
    /// Features included in the plan (comma-separated or JSON string)
    /// </summary>
    public string? Features { get; set; }

    /// <summary>
    /// When the subscription was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

// Hospital Registration-related events

/// <summary>
/// Base class for hospital registration events containing common properties
/// </summary>
public abstract class HospitalRegistrationEventBase : IntegrationEvent
{
    public Guid RegistrationId { get; set; }

    // Representative Information
    public string RepresentativeName { get; set; } = string.Empty;
    public string RepresentativeEmail { get; set; } = string.Empty;
    public string RepresentativePhone { get; set; } = string.Empty;

    // Hospital Information
    public string HospitalName { get; set; } = string.Empty;
    public string HospitalEmail { get; set; } = string.Empty;
    public string HospitalPhone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string TaxCode { get; set; } = string.Empty;
}

/// <summary>
/// Event published when a new hospital partnership registration is submitted
/// This event is consumed by Communication Service to send confirmation email
/// </summary>
public class HospitalRegistrationSubmittedEvent : HospitalRegistrationEventBase
{
    public DateTime SubmittedAt { get; set; }
}

/// <summary>
/// Event published when hospital registration status is updated by admin
/// This event is consumed by Communication Service to send status update email
/// </summary>
public class HospitalRegistrationStatusUpdatedEvent : HospitalRegistrationEventBase
{
    public int Status { get; set; } // 0=PENDING, 1=CONFIRMED, 2=CANCELLED
    public string StatusText { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? ContractFileUrl { get; set; }
    public Guid? HospitalId { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Event published to upload hospital registration files asynchronously to S3
/// This event is consumed by Hospital Service itself to process file uploads in background
/// </summary>
public class HospitalRegistrationFilesUploadEvent : IntegrationEvent
{
    public Guid RegistrationId { get; set; }
    public FileUploadData LicenseFile { get; set; } = null!;
    public FileUploadData BusinessCertificateFile { get; set; } = null!;
    public FileUploadData IdentityCardFile { get; set; } = null!;
}

/// <summary>
/// Event published when hospital account creation is requested (triggers Saga)
/// </summary>
public class HospitalAccountCreationRequestedEvent : HospitalRegistrationEventBase
{
    public string ContractFileUrl { get; set; } = string.Empty;
    public string GeneratedPassword { get; set; } = string.Empty;
}

/// <summary>
/// Event published when hospital account is successfully created
/// </summary>
public class HospitalAccountCreatedEvent : HospitalRegistrationEventBase
{
    public Guid AccountId { get; set; }
    public Guid HospitalId { get; set; }
    public string GeneratedPassword { get; set; } = string.Empty;
    public string LoginUrl { get; set; } = string.Empty;
    public string ContractFileUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Event published when a hospital successfully upgrades their subscription plan
/// This event is consumed by Notification Service to send upgrade confirmation email to hospital
/// </summary>
public class HospitalSubscriptionUpgradedEvent : IntegrationEvent
{
    /// <summary>
    /// ID of the new hospital subscription
    /// </summary>
    public Guid NewHospitalSubscriptionId { get; set; }

    /// <summary>
    /// ID of the previous hospital subscription (now cancelled)
    /// </summary>
    public Guid PreviousHospitalSubscriptionId { get; set; }

    /// <summary>
    /// ID of the hospital
    /// </summary>
    public Guid HospitalId { get; set; }

    /// <summary>
    /// Hospital name
    /// </summary>
    public string HospitalName { get; set; } = string.Empty;

    /// <summary>
    /// Hospital email for notification
    /// </summary>
    public string HospitalEmail { get; set; } = string.Empty;

    /// <summary>
    /// Contact person name
    /// </summary>
    public string ContactPersonName { get; set; } = string.Empty;

    /// <summary>
    /// Previous subscription plan name
    /// </summary>
    public string PreviousPlanName { get; set; } = string.Empty;

    /// <summary>
    /// Previous billing cycle
    /// </summary>
    public string PreviousBillingCycle { get; set; } = string.Empty;

    /// <summary>
    /// Previous subscription price
    /// </summary>
    public decimal PreviousPrice { get; set; }

    /// <summary>
    /// New subscription plan ID
    /// </summary>
    public Guid NewSubscriptionPlanId { get; set; }

    /// <summary>
    /// New subscription plan name
    /// </summary>
    public string NewPlanName { get; set; } = string.Empty;

    /// <summary>
    /// New billing cycle
    /// </summary>
    public string NewBillingCycle { get; set; } = string.Empty;

    /// <summary>
    /// New subscription price
    /// </summary>
    public decimal NewPrice { get; set; }

    /// <summary>
    /// New start date
    /// </summary>
    public DateTime NewStartDate { get; set; }

    /// <summary>
    /// New end date (includes bonus days from previous subscription)
    /// </summary>
    public DateTime NewEndDate { get; set; }

    /// <summary>
    /// Bonus days credited from previous subscription
    /// </summary>
    public double BonusDays { get; set; }

    /// <summary>
    /// New maximum number of doctors allowed
    /// </summary>
    public int? NewMaxDoctors { get; set; }

    /// <summary>
    /// New maximum number of appointments allowed per month
    /// </summary>
    public int? NewMaxAppointmentsPerMonth { get; set; }

    /// <summary>
    /// New features included in the plan
    /// </summary>
    public string? NewFeatures { get; set; }

    /// <summary>
    /// When the upgrade was completed
    /// </summary>
    public DateTime UpgradedAt { get; set; }
}

/// <summary>
/// Event published when hospital account creation failed (Saga compensation)
/// </summary>
public class HospitalAccountCreationFailedEvent : IntegrationEvent
{
    public Guid RegistrationId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime FailedAt { get; set; }
}

/// <summary>
/// Event published to update hospital registration with created hospital account details
/// This event is consumed by Hospital Service to link the registration with the created hospital account
/// </summary>
public class HospitalRegistrationAccountLinkedEvent : IntegrationEvent
{
    public Guid RegistrationId { get; set; }
    public Guid HospitalId { get; set; }
    public Guid AccountId { get; set; }
    public DateTime LinkedAt { get; set; }
}

/// <summary>
/// Generic event for creating in-app notifications
/// This event can be published by any service to request notification creation in Notification Service
/// Uses shared enums from BookingCare.Shared.Common.Enums for type safety
/// Supports bilingual content (Vietnamese + English)
/// Uses composition pattern to avoid code duplication
/// </summary>
public class CreateInAppNotificationEvent : IntegrationEvent
{
    /// <summary>
    /// User ID to send notification to
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Notification type (uses shared enum)
    /// </summary>
    public NotificationType Type { get; set; } = NotificationType.General;

    /// <summary>
    /// Notification content (title, content, metadata, etc.)
    /// Uses composition to avoid code duplication with NotificationContent
    /// </summary>
    public NotificationContent Content { get; set; } = new();
}

/// <summary>
/// File upload data container for event
/// </summary>
public class FileUploadData
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] FileData { get; set; } = Array.Empty<byte>();
    public string Folder { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
}

/// <summary>
/// Event published when admin generates a contract for hospital registration
/// </summary>
public class HospitalContractGeneratedEvent : HospitalRegistrationEventBase
{
    /// <summary>
    /// Contract number (e.g., HĐHT-2024-001)
    /// </summary>
    public string ContractNumber { get; set; } = string.Empty;

    /// <summary>
    /// URL of the contract draft file
    /// </summary>
    public string ContractDraftUrl { get; set; } = string.Empty;

    /// <summary>
    /// Signing link for hospital to sign the contract
    /// </summary>
    public string SigningLink { get; set; } = string.Empty;

    /// <summary>
    /// When the signing link expires
    /// </summary>
    public DateTime LinkExpiresAt { get; set; }
}

/// <summary>
/// Event published when a hospital signs the partnership contract
/// </summary>
public class HospitalContractSignedEvent : HospitalRegistrationEventBase
{
    /// <summary>
    /// Contract number (e.g., HĐHT-2024-001)
    /// </summary>
    public string ContractNumber { get; set; } = string.Empty;

    /// <summary>
    /// When the contract was signed
    /// </summary>
    public DateTime SignedAt { get; set; }

    /// <summary>
    /// URL of the signed contract file
    /// </summary>
    public string SignedContractUrl { get; set; } = string.Empty;
}

