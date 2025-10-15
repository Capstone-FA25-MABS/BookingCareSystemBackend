using BookingCare.Shared.EventBus.Events;
using BookingCare.Shared.Common.Interfaces;
using BookingCare.Shared.Common.Extensions;

namespace BookingCare.Services.Notification.Models.DTOs;

/// <summary>
/// DTO containing appointment booking email data
/// Used to resolve SonarQube issue: Method has too many parameters
/// Refactored to eliminate duplication with AppointmentBookingSuccessNotificationEvent
/// 
/// Uses composition with AppointmentData to eliminate SonarQube "Duplicated Lines" issue.
/// No duplicate property declarations - all contained in AppointmentData object!
/// 
/// Usage Examples:
/// 
/// 1. From Event (Recommended):
///    var emailData = AppointmentBookingEmailData.FromEvent(appointmentEvent);
/// 
/// 2. Direct access to appointment data:
///    var emailData = new AppointmentBookingEmailData();
///    emailData.AppointmentData.PatientName = "John Doe";
///    // OR use convenience properties:
///    emailData.PatientName = "John Doe"; // delegates to AppointmentData
/// 
/// 3. Using Builder Pattern:
///    var emailData = AppointmentBookingEmailData
///        .CreateBuilder("John Doe", DateTime.Now, "08:00 - 09:00")
///        .WithDoctor("Dr. Smith", "Cardiology")
///        .WithHospital("Central Hospital", "123 Main St")
///        .WithService("Consultation")
///        .WithAmount(500000)
///        .WithAppointmentType("CONSULTATION")
///        .Build();
/// </summary>
public class AppointmentBookingEmailData
{
    /// <summary>
    /// Appointment data - eliminates duplicate property declarations
    /// All appointment-related properties are contained in this object
    /// </summary>
    public AppointmentData AppointmentData { get; set; } = new();

    // Convenience properties for backward compatibility - delegate to AppointmentData
    /// <summary>
    /// Patient name (delegates to AppointmentData)
    /// </summary>
    public string PatientName
    {
        get => AppointmentData.PatientName;
        set => AppointmentData.PatientName = value;
    }

    /// <summary>
    /// Appointment date (delegates to AppointmentData)
    /// </summary>
    public DateTime AppointmentDate
    {
        get => AppointmentData.AppointmentDate;
        set => AppointmentData.AppointmentDate = value;
    }

    /// <summary>
    /// Appointment time (delegates to AppointmentData)
    /// </summary>
    public string AppointmentTime
    {
        get => AppointmentData.AppointmentTime;
        set => AppointmentData.AppointmentTime = value;
    }

    /// <summary>
    /// Doctor name (delegates to AppointmentData)
    /// </summary>
    public string? DoctorName
    {
        get => AppointmentData.DoctorName;
        set => AppointmentData.DoctorName = value;
    }

    /// <summary>
    /// Doctor specialty (delegates to AppointmentData)
    /// </summary>
    public string? DoctorSpecialty
    {
        get => AppointmentData.DoctorSpecialty;
        set => AppointmentData.DoctorSpecialty = value;
    }

    /// <summary>
    /// Hospital name (delegates to AppointmentData)
    /// </summary>
    public string? HospitalName
    {
        get => AppointmentData.HospitalName;
        set => AppointmentData.HospitalName = value;
    }

    /// <summary>
    /// Hospital address (delegates to AppointmentData)
    /// </summary>
    public string? HospitalAddress
    {
        get => AppointmentData.HospitalAddress;
        set => AppointmentData.HospitalAddress = value;
    }

    /// <summary>
    /// Service name (delegates to AppointmentData)
    /// </summary>
    public string? ServiceName
    {
        get => AppointmentData.ServiceName;
        set => AppointmentData.ServiceName = value;
    }

    /// <summary>
    /// Payment amount (delegates to AppointmentData)
    /// </summary>
    public decimal Amount
    {
        get => AppointmentData.Amount;
        set => AppointmentData.Amount = value;
    }

    /// <summary>
    /// Appointment type (delegates to AppointmentData)
    /// </summary>
    public string AppointmentType
    {
        get => AppointmentData.AppointmentType;
        set => AppointmentData.AppointmentType = value;
    }

    /// <summary>
    /// Creates AppointmentBookingEmailData from AppointmentBookingSuccessNotificationEvent
    /// Uses composition to share AppointmentData object
    /// </summary>
    /// <param name="eventData">The appointment booking success notification event</param>
    /// <returns>Mapped AppointmentBookingEmailData</returns>
    public static AppointmentBookingEmailData FromEvent(AppointmentBookingSuccessNotificationEvent eventData)
    {
        return new AppointmentBookingEmailData
        {
            AppointmentData = eventData.AppointmentData // Direct assignment - no duplication!
        };
    }

    /// <summary>
    /// Creates AppointmentBookingEmailData from AppointmentDataBase
    /// </summary>
    /// <param name="appointmentData">The appointment data source</param>
    /// <returns>Mapped AppointmentBookingEmailData</returns>
    public static AppointmentBookingEmailData FromAppointmentData(AppointmentDataBase appointmentData)
    {
        var emailData = new AppointmentBookingEmailData();
        emailData.AppointmentData.CopyFrom(appointmentData); // Uses extension method
        return emailData;
    }

    /// <summary>
    /// Creates a builder for constructing AppointmentBookingEmailData with fluent API
    /// This approach avoids SonarQube issues with too many parameters
    /// </summary>
    /// <param name="patientName">Required patient name</param>
    /// <param name="appointmentDate">Required appointment date</param>
    /// <param name="appointmentTime">Required appointment time</param>
    /// <returns>Builder instance for fluent configuration</returns>
    public static Builder CreateBuilder(string patientName, DateTime appointmentDate, string appointmentTime)
    {
        return new Builder(patientName, appointmentDate, appointmentTime);
    }

    /// <summary>
    /// Builder class for fluent construction of AppointmentBookingEmailData
    /// Complies with SonarQube rules by avoiding methods with too many parameters
    /// </summary>
    public class Builder
    {
        private readonly AppointmentBookingEmailData _data;

        internal Builder(string patientName, DateTime appointmentDate, string appointmentTime)
        {
            _data = new AppointmentBookingEmailData
            {
                PatientName = patientName,        // Uses property delegation
                AppointmentDate = appointmentDate, // Uses property delegation
                AppointmentTime = appointmentTime  // Uses property delegation
            };
        }

        /// <summary>
        /// Sets the doctor information
        /// </summary>
        /// <param name="doctorName">Doctor's name</param>
        /// <param name="doctorSpecialty">Doctor's specialty (optional)</param>
        /// <returns>Builder instance for fluent configuration</returns>
        public Builder WithDoctor(string? doctorName, string? doctorSpecialty = null)
        {
            _data.DoctorName = doctorName;           // Uses property delegation
            _data.DoctorSpecialty = doctorSpecialty; // Uses property delegation
            return this;
        }

        /// <summary>
        /// Sets the hospital information
        /// </summary>
        /// <param name="hospitalName">Hospital's name</param>
        /// <param name="hospitalAddress">Hospital's address (optional)</param>
        /// <returns>Builder instance for fluent configuration</returns>
        public Builder WithHospital(string? hospitalName, string? hospitalAddress = null)
        {
            _data.HospitalName = hospitalName;       // Uses property delegation
            _data.HospitalAddress = hospitalAddress; // Uses property delegation
            return this;
        }

        /// <summary>
        /// Sets the service information
        /// </summary>
        /// <param name="serviceName">Service name</param>
        /// <returns>Builder instance for fluent configuration</returns>
        public Builder WithService(string? serviceName)
        {
            _data.ServiceName = serviceName; // Uses property delegation
            return this;
        }

        /// <summary>
        /// Sets the payment amount
        /// </summary>
        /// <param name="amount">Payment amount</param>
        /// <returns>Builder instance for fluent configuration</returns>
        public Builder WithAmount(decimal amount)
        {
            _data.Amount = amount; // Uses property delegation
            return this;
        }

        /// <summary>
        /// Sets the appointment type
        /// </summary>
        /// <param name="appointmentType">Appointment type</param>
        /// <returns>Builder instance for fluent configuration</returns>
        public Builder WithAppointmentType(string appointmentType)
        {
            _data.AppointmentType = appointmentType; // Uses property delegation
            return this;
        }

        /// <summary>
        /// Builds the AppointmentBookingEmailData object
        /// </summary>
        /// <returns>Constructed AppointmentBookingEmailData</returns>
        public AppointmentBookingEmailData Build()
        {
            return _data;
        }
    }
}