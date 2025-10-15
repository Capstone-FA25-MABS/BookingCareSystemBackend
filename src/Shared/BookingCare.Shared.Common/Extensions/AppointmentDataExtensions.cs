using BookingCare.Shared.Common.Interfaces;

namespace BookingCare.Shared.Common.Extensions;

/// <summary>
/// Extension methods for IAppointmentData to help with conversions
/// Used to eliminate SonarQube "Duplicated Lines" issue
/// </summary>
public static class AppointmentDataExtensions
{
    /// <summary>
    /// Copies appointment data from one IAppointmentData to another
    /// This eliminates the need for duplicate property assignments
    /// </summary>
    /// <param name="target">Target appointment data object</param>
    /// <param name="source">Source appointment data object</param>
    public static void CopyFrom(this IAppointmentData target, IAppointmentData source)
    {
        target.PatientName = source.PatientName;
        target.AppointmentDate = source.AppointmentDate;
        target.AppointmentTime = source.AppointmentTime;
        target.DoctorName = source.DoctorName;
        target.DoctorSpecialty = source.DoctorSpecialty;
        target.HospitalName = source.HospitalName;
        target.HospitalAddress = source.HospitalAddress;
        target.ServiceName = source.ServiceName;
        target.Amount = source.Amount;
        target.AppointmentType = source.AppointmentType;
    }

    /// <summary>
    /// Creates a generic appointment data object from IAppointmentData
    /// </summary>
    /// <param name="source">Source appointment data</param>
    /// <returns>Generic appointment data implementation</returns>
    public static AppointmentData ToAppointmentData(this IAppointmentData source)
    {
        return new AppointmentData
        {
            PatientName = source.PatientName,
            AppointmentDate = source.AppointmentDate,
            AppointmentTime = source.AppointmentTime,
            DoctorName = source.DoctorName,
            DoctorSpecialty = source.DoctorSpecialty,
            HospitalName = source.HospitalName,
            HospitalAddress = source.HospitalAddress,
            ServiceName = source.ServiceName,
            Amount = source.Amount,
            AppointmentType = source.AppointmentType
        };
    }
}

/// <summary>
/// Generic implementation of IAppointmentData
/// Used for data conversion and manipulation
/// </summary>
public class AppointmentData : IAppointmentData
{
    public string PatientName { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public string AppointmentTime { get; set; } = string.Empty;
    public string? DoctorName { get; set; }
    public string? DoctorSpecialty { get; set; }
    public string? HospitalName { get; set; }
    public string? HospitalAddress { get; set; }
    public string? ServiceName { get; set; }
    public decimal Amount { get; set; }
    public string AppointmentType { get; set; } = string.Empty;
}