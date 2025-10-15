using BookingCare.Shared.EventBus.Abstractions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Services.Notification.Utils.Email;

namespace BookingCare.Services.Notification.Handlers;

/// <summary>
/// Event handler for processing appointment booking success notifications
/// Sends success email to patient when appointment is booked and payment is completed
/// </summary>
public class AppointmentBookingSuccessNotificationEventHandler : IIntegrationEventHandler<AppointmentBookingSuccessNotificationEvent>
{
    private readonly EmailService _emailService;
    private readonly ILogger<AppointmentBookingSuccessNotificationEventHandler> _logger;

    public AppointmentBookingSuccessNotificationEventHandler(
        EmailService emailService,
        ILogger<AppointmentBookingSuccessNotificationEventHandler> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task HandleAsync(AppointmentBookingSuccessNotificationEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[NotificationService] Received AppointmentBookingSuccessNotificationEvent - AppointmentId: {AppointmentId}, PatientId: {PatientId}, PatientEmail: {PatientEmail}, CorrelationId: {CorrelationId}",
            @event.AppointmentId, @event.PatientId, @event.PatientEmail, @event.CorrelationId);

        try
        {
            // Validate required fields
            if (string.IsNullOrEmpty(@event.PatientEmail))
            {
                _logger.LogWarning(
                    "[NotificationService] Patient email is empty for appointment booking success notification - AppointmentId: {AppointmentId}",
                    @event.AppointmentId);
                return;
            }

            if (string.IsNullOrEmpty(@event.PatientName))
            {
                @event.PatientName = "Quý khách"; // Default fallback
            }

            // Generate HTML email content using the template
            var emailContent = EmailTemplate.BuildAppointmentBookedSuccessEmailHtml(
                patientName: @event.PatientName,
                appointmentDate: @event.AppointmentDate,
                appointmentTime: @event.AppointmentTime,
                doctorName: @event.DoctorName,
                doctorSpecialty: @event.DoctorSpecialty,
                hospitalName: @event.HospitalName,
                hospitalAddress: @event.HospitalAddress,
                serviceName: @event.ServiceName,
                amount: @event.Amount,
                appointmentType: @event.AppointmentType
            );

            // Send the email
            await _emailService.SendEmailAsync(
                toEmail: @event.PatientEmail,
                subject: @event.EmailSubject,
                content: emailContent,
                isHtml: true,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "[NotificationService] Successfully sent appointment booking success email - AppointmentId: {AppointmentId}, PatientEmail: {PatientEmail}",
                @event.AppointmentId, @event.PatientEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[NotificationService] Failed to send appointment booking success email - AppointmentId: {AppointmentId}, PatientEmail: {PatientEmail}",
                @event.AppointmentId, @event.PatientEmail);

            // Don't re-throw to avoid breaking the event processing pipeline
            // The error is already logged for investigation
        }
    }
}