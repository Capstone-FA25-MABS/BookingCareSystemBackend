using BookingCare.Shared.Common.Exceptions;

namespace BookingCare.Services.Review.Exceptions;

/// <summary>
/// Exception thrown when a patient tries to create a duplicate review
/// </summary>
public class DuplicateReviewException : BusinessException
{
    public Guid PatientId { get; }
    public Guid? DoctorId { get; }
    public Guid? ServiceId { get; }
    public string ExistingReviewId { get; }

    public DuplicateReviewException(Guid patientId, Guid? doctorId, Guid? serviceId, string existingReviewId, string targetName)
        : base($"Patient has already reviewed {targetName}. Please update the existing review instead of creating a new one.")
    {
        PatientId = patientId;
        DoctorId = doctorId;
        ServiceId = serviceId;
        ExistingReviewId = existingReviewId;
    }
}