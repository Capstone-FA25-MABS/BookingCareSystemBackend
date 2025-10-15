using BookingCare.Shared.EventBus.Events;

namespace BookingCare.Services.Notification.Models.DTOs;

/// <summary>
/// DTO containing appointment booking email data
/// Used to resolve SonarQube issue: Method has too many parameters
/// Refactored to eliminate duplication with AppointmentBookingSuccessNotificationEvent
/// 
/// Usage Examples:
/// 
/// 1. From Event (Recommended):
///    var emailData = AppointmentBookingEmailData.FromEvent(appointmentEvent);
/// 
/// 2. Using Builder Pattern:
///    var emailData = AppointmentBookingEmailData
///        .CreateBuilder("John Doe", DateTime.Now, "08:00 - 09:00")
///        .WithDoctor("Dr. Smith", "Cardiology")
///        .WithHospital("Central Hospital", "123 Main St")
///        .WithService("Consultation")
///        .WithAmount(500000)
///        .WithAppointmentType("CONSULTATION")
///        .Build();
/// 
/// 3. Minimal Builder Usage:
///    var emailData = AppointmentBookingEmailData
///        .CreateBuilder("Jane Doe", DateTime.Now, "10:00 - 11:00")
///        .WithAmount(300000)
///        .Build();
/// </summary>
public class AppointmentBookingEmailData
{
    /// <summary>
    /// Patient name
    /// </summary>
    public string PatientName { get; set; } = string.Empty;

    /// <summary>
    /// Appointment date
    /// </summary>
    public DateTime AppointmentDate { get; set; }

    /// <summary>
    /// Appointment time (formatted string)
    /// </summary>
    public string AppointmentTime { get; set; } = string.Empty;

    /// <summary>
    /// Doctor name (optional)
    /// </summary>
    public string? DoctorName { get; set; }

    /// <summary>
    /// Doctor specialty (optional)
    /// </summary>
    public string? DoctorSpecialty { get; set; }

    /// <summary>
    /// Hospital name (optional)
    /// </summary>
    public string? HospitalName { get; set; }

    /// <summary>
    /// Hospital address (optional)
    /// </summary>
    public string? HospitalAddress { get; set; }

    /// <summary>
    /// Service name (optional)
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Payment amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Appointment type
    /// </summary>
    public string AppointmentType { get; set; } = string.Empty;

    /// <summary>
    /// Creates AppointmentBookingEmailData from AppointmentBookingSuccessNotificationEvent
    /// This eliminates duplication between the Event and DTO
    /// </summary>
    /// <param name="eventData">The appointment booking success notification event</param>
    /// <returns>Mapped AppointmentBookingEmailData</returns>
    public static AppointmentBookingEmailData FromEvent(AppointmentBookingSuccessNotificationEvent eventData)
    {
        return new AppointmentBookingEmailData
        {
            PatientName = eventData.PatientName,
            AppointmentDate = eventData.AppointmentDate,
            AppointmentTime = eventData.AppointmentTime,
            DoctorName = eventData.DoctorName,
            DoctorSpecialty = eventData.DoctorSpecialty,
            HospitalName = eventData.HospitalName,
            HospitalAddress = eventData.HospitalAddress,
            ServiceName = eventData.ServiceName,
            Amount = eventData.Amount,
            AppointmentType = eventData.AppointmentType
        };
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
                PatientName = patientName,
                AppointmentDate = appointmentDate,
                AppointmentTime = appointmentTime
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
            _data.DoctorName = doctorName;
            _data.DoctorSpecialty = doctorSpecialty;
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
            _data.HospitalName = hospitalName;
            _data.HospitalAddress = hospitalAddress;
            return this;
        }

        /// <summary>
        /// Sets the service information
        /// </summary>
        /// <param name="serviceName">Service name</param>
        /// <returns>Builder instance for fluent configuration</returns>
        public Builder WithService(string? serviceName)
        {
            _data.ServiceName = serviceName;
            return this;
        }

        /// <summary>
        /// Sets the payment amount
        /// </summary>
        /// <param name="amount">Payment amount</param>
        /// <returns>Builder instance for fluent configuration</returns>
        public Builder WithAmount(decimal amount)
        {
            _data.Amount = amount;
            return this;
        }

        /// <summary>
        /// Sets the appointment type
        /// </summary>
        /// <param name="appointmentType">Appointment type</param>
        /// <returns>Builder instance for fluent configuration</returns>
        public Builder WithAppointmentType(string appointmentType)
        {
            _data.AppointmentType = appointmentType;
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