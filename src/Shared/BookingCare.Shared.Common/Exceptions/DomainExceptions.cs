using System.Net;

namespace BookingCare.Shared.Common.Exceptions.Domain;

/// <summary>
/// Authentication and authorization related exceptions
/// </summary>
public static class AuthExceptions
{
    public class InvalidCredentialsException : UnauthorizedException
    {
        public InvalidCredentialsException(string message = "Invalid credentials provided")
            : base(message, "INVALID_CREDENTIALS")
        {
        }
    }

    public class TokenExpiredException : UnauthorizedException
    {
        public TokenExpiredException(string message = "Authentication token has expired")
            : base(message, "TOKEN_EXPIRED")
        {
        }
    }

    public class InvalidTokenException : UnauthorizedException
    {
        public InvalidTokenException(string message = "Invalid authentication token")
            : base(message, "INVALID_TOKEN")
        {
        }
    }

    public class AccountLockedException : ForbiddenException
    {
        public AccountLockedException(string message = "Account is locked")
            : base(message, "ACCOUNT_LOCKED")
        {
        }
    }

    public class EmailNotVerifiedException : ForbiddenException
    {
        public EmailNotVerifiedException(string message = "Email address is not verified")
            : base(message, "EMAIL_NOT_VERIFIED")
        {
        }
    }
}

/// <summary>
/// User management related exceptions
/// </summary>
public static class UserExceptions
{
    public class UserAlreadyExistsException : ConflictException
    {
        public UserAlreadyExistsException(string identifier, string field = "email")
            : base($"User with {field} '{identifier}' already exists", "USER_ALREADY_EXISTS")
        {
            Details["Field"] = field;
            Details["Value"] = identifier;
        }
    }

    public class UserNotFoundException : NotFoundException
    {
        public UserNotFoundException(object identifier)
            : base("User", identifier, "USER_NOT_FOUND")
        {
        }
    }

    public class InvalidUserRoleException : BusinessException
    {
        public InvalidUserRoleException(string role)
            : base($"Invalid user role: {role}", "INVALID_USER_ROLE")
        {
            Details["Role"] = role;
        }
    }

    public class ProfileIncompleteException : BusinessException
    {
        public ProfileIncompleteException(List<string> missingFields)
            : base("User profile is incomplete", "PROFILE_INCOMPLETE")
        {
            Details["MissingFields"] = missingFields;
        }
    }
}

/// <summary>
/// Clinic and healthcare facility related exceptions
/// </summary>
public static class ClinicExceptions
{
    public class ClinicNotFoundException : NotFoundException
    {
        public ClinicNotFoundException(object identifier)
            : base("Clinic", identifier, "CLINIC_NOT_FOUND")
        {
        }
    }

    public class ClinicNotActiveException : BusinessException
    {
        public ClinicNotActiveException(Guid clinicId)
            : base($"Clinic with ID {clinicId} is not active", "CLINIC_NOT_ACTIVE")
        {
            Details["ClinicId"] = clinicId;
        }
    }

    public class InvalidOperatingHoursException : ValidationException
    {
        public InvalidOperatingHoursException(string message = "Invalid operating hours specified")
            : base(message, null, "INVALID_OPERATING_HOURS")
        {
        }
    }

    public class ClinicCapacityExceededException : BusinessException
    {
        public ClinicCapacityExceededException(string message = "Clinic capacity exceeded")
            : base(message, "CLINIC_CAPACITY_EXCEEDED")
        {
        }
    }
}

/// <summary>
/// Doctor and medical professional related exceptions
/// </summary>
public static class DoctorExceptions
{
    public class DoctorNotFoundException : NotFoundException
    {
        public DoctorNotFoundException(object identifier)
            : base("Doctor", identifier, "DOCTOR_NOT_FOUND")
        {
        }
    }

    public class DoctorNotAvailableException : BusinessException
    {
        public DoctorNotAvailableException(Guid doctorId, DateTime requestedTime)
            : base($"Doctor is not available at the requested time", "DOCTOR_NOT_AVAILABLE")
        {
            Details["DoctorId"] = doctorId;
            Details["RequestedTime"] = requestedTime;
        }
    }

    public class InvalidMedicalLicenseException : ValidationException
    {
        public InvalidMedicalLicenseException(string licenseNumber)
            : base($"Invalid medical license number: {licenseNumber}", null, "INVALID_MEDICAL_LICENSE")
        {
            Details["LicenseNumber"] = licenseNumber;
        }
    }

    public class SpecializationNotFoundException : NotFoundException
    {
        public SpecializationNotFoundException(string specialization)
            : base($"Specialization '{specialization}' not found", "SPECIALIZATION_NOT_FOUND")
        {
            Details["Specialization"] = specialization;
        }
    }
}

/// <summary>
/// Appointment booking and management related exceptions
/// </summary>
public static class AppointmentExceptions
{
    public class AppointmentNotFoundException : NotFoundException
    {
        public AppointmentNotFoundException(object identifier)
            : base("Appointment", identifier, "APPOINTMENT_NOT_FOUND")
        {
        }
    }

    public class AppointmentConflictException : ConflictException
    {
        public AppointmentConflictException(DateTime appointmentTime, Guid doctorId)
            : base($"Appointment conflict at {appointmentTime:yyyy-MM-dd HH:mm}", "APPOINTMENT_CONFLICT")
        {
            Details["AppointmentTime"] = appointmentTime;
            Details["DoctorId"] = doctorId;
        }
    }

    public class InvalidAppointmentTimeException : ValidationException
    {
        public InvalidAppointmentTimeException(string message = "Invalid appointment time")
            : base(message, null, "INVALID_APPOINTMENT_TIME")
        {
        }
    }

    public class AppointmentCancellationException : BusinessException
    {
        public AppointmentCancellationException(string reason)
            : base($"Cannot cancel appointment: {reason}", "APPOINTMENT_CANCELLATION_ERROR")
        {
            Details["Reason"] = reason;
        }
    }

    public class AppointmentAlreadyBookedException : ConflictException
    {
        public AppointmentAlreadyBookedException(DateTime appointmentTime)
            : base($"Appointment slot at {appointmentTime:yyyy-MM-dd HH:mm} is already booked", "APPOINTMENT_ALREADY_BOOKED")
        {
            Details["AppointmentTime"] = appointmentTime;
        }
    }

    public class PastAppointmentException : BusinessException
    {
        public PastAppointmentException(string action = "modify")
            : base($"Cannot {action} past appointments", "PAST_APPOINTMENT_ERROR")
        {
            Details["Action"] = action;
        }
    }
}

/// <summary>
/// Payment and billing related exceptions
/// </summary>
public static class PaymentExceptions
{
    public class PaymentNotFoundException : NotFoundException
    {
        public PaymentNotFoundException(object identifier)
            : base("Payment", identifier, "PAYMENT_NOT_FOUND")
        {
        }
    }

    public class PaymentFailedException : BusinessException
    {
        public PaymentFailedException(string reason, string? transactionId = null)
            : base($"Payment failed: {reason}", "PAYMENT_FAILED")
        {
            if (!string.IsNullOrEmpty(transactionId))
                Details["TransactionId"] = transactionId;
            Details["Reason"] = reason;
        }
    }

    public class InsufficientFundsException : BusinessException
    {
        public InsufficientFundsException(decimal requiredAmount, decimal availableAmount)
            : base($"Insufficient funds. Required: {requiredAmount:C}, Available: {availableAmount:C}", "INSUFFICIENT_FUNDS")
        {
            Details["RequiredAmount"] = requiredAmount;
            Details["AvailableAmount"] = availableAmount;
        }
    }

    public class RefundNotAllowedException : BusinessException
    {
        public RefundNotAllowedException(string reason)
            : base($"Refund not allowed: {reason}", "REFUND_NOT_ALLOWED")
        {
            Details["Reason"] = reason;
        }
    }
}

/// <summary>
/// Communication and notification related exceptions
/// </summary>
public static class CommunicationExceptions
{
    public class EmailDeliveryException : ExternalServiceException
    {
        public EmailDeliveryException(string email, string reason, Exception? innerException = null)
            : base("Email Service", $"Failed to deliver email to {email}: {reason}", "EMAIL_DELIVERY_FAILED", innerException)
        {
            Details["Email"] = email;
            Details["Reason"] = reason;
        }
    }

    public class SmsDeliveryException : ExternalServiceException
    {
        public SmsDeliveryException(string phoneNumber, string reason, Exception? innerException = null)
            : base("SMS Service", $"Failed to deliver SMS to {phoneNumber}: {reason}", "SMS_DELIVERY_FAILED", innerException)
        {
            Details["PhoneNumber"] = phoneNumber;
            Details["Reason"] = reason;
        }
    }

    public class NotificationTemplateNotFoundException : NotFoundException
    {
        public NotificationTemplateNotFoundException(string templateName)
            : base($"Notification template '{templateName}' not found", "NOTIFICATION_TEMPLATE_NOT_FOUND")
        {
            Details["TemplateName"] = templateName;
        }
    }
}

/// <summary>
/// Content and review related exceptions
/// </summary>
public static class ContentExceptions
{
    public class ReviewNotFoundException : NotFoundException
    {
        public ReviewNotFoundException(object identifier)
            : base("Review", identifier, "REVIEW_NOT_FOUND")
        {
        }
    }

    public class DuplicateReviewException : ConflictException
    {
        public DuplicateReviewException(Guid userId, Guid entityId)
            : base("User has already reviewed this entity", "DUPLICATE_REVIEW")
        {
            Details["UserId"] = userId;
            Details["EntityId"] = entityId;
        }
    }

    public class InvalidRatingException : ValidationException
    {
        public InvalidRatingException(int rating, int minRating = 1, int maxRating = 5)
            : base($"Rating {rating} is invalid. Must be between {minRating} and {maxRating}", null, "INVALID_RATING")
        {
            Details["Rating"] = rating;
            Details["MinRating"] = minRating;
            Details["MaxRating"] = maxRating;
        }
    }

    public class ContentModerationException : BusinessException
    {
        public ContentModerationException(string reason)
            : base($"Content failed moderation: {reason}", "CONTENT_MODERATION_FAILED")
        {
            Details["Reason"] = reason;
        }
    }
}
