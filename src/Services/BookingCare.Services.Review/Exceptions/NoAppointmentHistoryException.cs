using BookingCare.Shared.Common.Exceptions;

namespace BookingCare.Services.Review.Exceptions;

/// <summary>
/// Exception thrown when a patient tries to review without completed appointment
/// </summary>
public class NoAppointmentHistoryException : BusinessException
{
    public Guid PatientId { get; }
    public Guid? DoctorId { get; }
    public Guid? ServiceId { get; }
    public string TargetType { get; }

    public NoAppointmentHistoryException(Guid patientId, Guid? doctorId, Guid? serviceId, string targetType)
  : base($"You must complete an appointment with this {targetType.ToLower()} before creating a review.")
    {
        PatientId = patientId;
        DoctorId = doctorId;
        ServiceId = serviceId;
        TargetType = targetType;
    }
}