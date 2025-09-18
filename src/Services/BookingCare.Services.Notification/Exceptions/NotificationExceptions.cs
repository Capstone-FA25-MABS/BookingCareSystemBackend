using System.Net;
using BookingCare.Shared.Common.Exceptions;

namespace BookingCare.Services.Notification.Exceptions;

#region Base Notification Exception

public class NotificationException : BookingCareException
{
    public NotificationException(
        string message,
        string errorCode = "NOTIFICATION_ERROR",
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError,
        Exception? innerException = null,
        Dictionary<string, object>? details = null)
        : base(message, errorCode, statusCode, innerException, details)
    {
    }
}

#endregion

#region OTP Exceptions

public class OtpException : NotificationException
{
    public OtpException(
        string message,
        string errorCode = "OTP_ERROR",
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError,
        Exception? innerException = null,
        Dictionary<string, object>? details = null)
        : base(message, errorCode, statusCode, innerException, details)
    {
    }
}

public class OtpValidationException : ValidationException
{
    public OtpValidationException(List<ValidationError> validationErrors)
        : base("OTP validation failed", validationErrors, "OTP_VALIDATION_ERROR")
    {
    }

    public OtpValidationException(string field, string message, object? attemptedValue = null)
        : base("OTP validation failed", new List<ValidationError>
        {
            new(field, message, attemptedValue)
        }, "OTP_VALIDATION_ERROR")
    {
    }
}


public class OtpInvalidException : BusinessException
{
    public OtpInvalidException(string providedOtp, bool withDetails)
        : base($"Invalid OTP code: '{providedOtp}'", "OTP_INVALID")
    {
        Details["ProvidedOtp"] = providedOtp;
    }
}

#endregion

#region FCM Exceptions

public class FcmException : NotificationException
{
    public FcmException(
        string message,
        string errorCode = "FCM_ERROR",
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError,
        Exception? innerException = null,
        Dictionary<string, object>? details = null)
        : base(message, errorCode, statusCode, innerException, details)
    {
    }
}

public class FcmConfigurationException : FcmException
{
    public FcmConfigurationException(string message, Exception? innerException = null)
        : base(message, "FCM_CONFIGURATION_ERROR", HttpStatusCode.InternalServerError, innerException)
    {
    }
}

public class FcmAuthenticationException : FcmException
{
    public FcmAuthenticationException(string message, Exception? innerException = null)
        : base(message, "FCM_AUTHENTICATION_ERROR", HttpStatusCode.Unauthorized, innerException)
    {
    }
}

public class FcmTokenValidationException : ValidationException
{
    public FcmTokenValidationException(string token, string validationMessage)
        : base($"Invalid FCM token: {validationMessage}", new List<ValidationError>
        {
            new("FcmToken", validationMessage, token)
        }, "FCM_TOKEN_VALIDATION_ERROR")
    {
    }
}

public class FcmDeliveryException : FcmException
{
    public FcmDeliveryException(string message, string token, Exception? innerException = null)
        : base(message, "FCM_DELIVERY_ERROR", HttpStatusCode.BadRequest, innerException)
    {
        Details["FcmToken"] = token;
    }

}

public class FcmServiceUnavailableException : FcmException
{
    public FcmServiceUnavailableException(string message, Exception? innerException = null)
        : base(message, "FCM_SERVICE_UNAVAILABLE", HttpStatusCode.ServiceUnavailable, innerException)
    {
    }
}

public class FcmQuotaExceededException : FcmException
{
    public FcmQuotaExceededException(string message, Exception? innerException = null)
        : base(message, "FCM_QUOTA_EXCEEDED", HttpStatusCode.TooManyRequests, innerException)
    {
    }
}

#endregion

#region Device Management Exceptions

public class DeviceException : NotificationException
{
    public DeviceException(
        string message,
        string errorCode = "DEVICE_ERROR",
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError,
        Exception? innerException = null,
        Dictionary<string, object>? details = null)
        : base(message, errorCode, statusCode, innerException, details)
    {
    }
}

public class DeviceRegistrationException : DeviceException
{
    public DeviceRegistrationException(string message, Exception? innerException = null)
        : base(message, "DEVICE_REGISTRATION_ERROR", HttpStatusCode.BadRequest, innerException)
    {
    }

}

public class DeviceValidationException : ValidationException
{
    public DeviceValidationException(string field, string message, object? attemptedValue = null)
        : base("Device validation failed", new List<ValidationError>
        {
            new(field, message, attemptedValue)
        }, "DEVICE_VALIDATION_ERROR")
    {
    }
}

#endregion

#region Email/SMS Exceptions

public class EmailDeliveryException : NotificationException
{
    public EmailDeliveryException(string message, Exception? innerException = null)
        : base(message, "EMAIL_DELIVERY_ERROR", HttpStatusCode.InternalServerError, innerException)
    {
    }

    public EmailDeliveryException(string message, string email, Exception? innerException = null)
        : base(message, "EMAIL_DELIVERY_ERROR", HttpStatusCode.InternalServerError, innerException)
    {
        Details["Email"] = email;
    }
}

public class SmsDeliveryException : NotificationException
{
    public SmsDeliveryException(string message, Exception? innerException = null)
        : base(message, "SMS_DELIVERY_ERROR", HttpStatusCode.InternalServerError, innerException)
    {
    }

    public SmsDeliveryException(string message, string phone, string deviceId, Exception? innerException = null)
        : base(message, "SMS_DELIVERY_ERROR", HttpStatusCode.InternalServerError, innerException)
    {
        Details["Phone"] = phone;
        Details["DeviceId"] = deviceId;
    }
}

#endregion
