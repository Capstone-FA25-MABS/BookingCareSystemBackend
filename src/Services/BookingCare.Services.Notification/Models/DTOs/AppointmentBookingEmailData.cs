using BookingCare.Shared.EventBus.Events;
using BookingCare.Shared.Common.Interfaces;
using BookingCare.Shared.Common.Extensions;

namespace BookingCare.Services.Notification.Models.DTOs;

/// <summary>
/// DTO containing appointment booking email data
/// Used to resolve SonarQube issue: Method has too many parameters
/// Refactored to eliminate duplication with AppointmentBookingSuccessNotificationEvent
/// 
/// Uses pure composition pattern - NO delegation properties to eliminate SonarQube "Duplicated Lines" issue.
/// 
/// Usage Examples:
/// 
/// 1. From Event (Recommended):
///    var emailData = AppointmentBookingEmailData.FromEvent(appointmentEvent);
/// 
/// 2. Direct access to appointment data:
///    var emailData = new AppointmentBookingEmailData();
///    emailData.AppointmentData.PatientName = "John Doe";
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
    /// Appointment data - contains all appointment-related information
    /// Access properties via: AppointmentData.PatientName, AppointmentData.AppointmentDate, etc.
    /// NO delegation properties to eliminate code duplication!
    /// </summary>
    public AppointmentData AppointmentData { get; set; } = new();

    // NO delegation properties here - completely eliminates duplicate code!
    // Access appointment data via: emailData.AppointmentData.PatientName, etc.

    /// <summary>
    /// Creates AppointmentBookingEmailData from AppointmentBookingSuccessNotificationEvent
    /// Uses direct AppointmentData assignment - no duplication!
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
            _data = new AppointmentBookingEmailData();
            _data.AppointmentData.PatientName = patientName;        // Direct access to AppointmentData
            _data.AppointmentData.AppointmentDate = appointmentDate; // Direct access to AppointmentData
            _data.AppointmentData.AppointmentTime = appointmentTime;  // Direct access to AppointmentData
        }

        /// <summary>
        /// Sets the doctor information
        /// </summary>
        /// <param name="doctorName">Doctor's name</param>
        /// <param name="doctorSpecialty">Doctor's specialty (optional)</param>
        /// <returns>Builder instance for fluent configuration</returns>
        public Builder WithDoctor(string? doctorName, string? doctorSpecialty = null)
        {
            _data.AppointmentData.DoctorName = doctorName;           // Direct access to AppointmentData
            _data.AppointmentData.DoctorSpecialty = doctorSpecialty; // Direct access to AppointmentData
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
            _data.AppointmentData.HospitalName = hospitalName;       // Direct access to AppointmentData
            _data.AppointmentData.HospitalAddress = hospitalAddress; // Direct access to AppointmentData
            return this;
        }

        /// <summary>
        /// Sets the service information
        /// </summary>
        /// <param name="serviceName">Service name</param>
        /// <returns>Builder instance for fluent configuration</returns>
        public Builder WithService(string? serviceName)
        {
            _data.AppointmentData.ServiceName = serviceName; // Direct access to AppointmentData
            return this;
        }

        /// <summary>
        /// Sets the payment amount
        /// </summary>
        /// <param name="amount">Payment amount</param>
        /// <returns>Builder instance for fluent configuration</returns>
        public Builder WithAmount(decimal amount)
        {
            _data.AppointmentData.Amount = amount; // Direct access to AppointmentData
            return this;
        }

        /// <summary>
        /// Sets the appointment type
        /// </summary>
        /// <param name="appointmentType">Appointment type</param>
        /// <returns>Builder instance for fluent configuration</returns>
        public Builder WithAppointmentType(string appointmentType)
        {
            _data.AppointmentData.AppointmentType = appointmentType; // Direct access to AppointmentData
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