using BookingCare.Shared.Common.Interfaces;

namespace BookingCare.Shared.Common.Extensions;

/// <summary>
/// Extension methods for appointment data to help with conversions
/// Used to eliminate SonarQube "Duplicated Lines" issue
/// Updated to work with AppointmentDataBase abstract class
/// </summary>
public static class AppointmentDataExtensions
{
    /// <summary>
    /// Copies appointment data from one AppointmentDataBase to another
    /// This eliminates the need for duplicate property assignments
    /// </summary>
    /// <param name="target">Target appointment data object</param>
    /// <param name="source">Source appointment data object</param>
    public static void CopyFrom(this AppointmentDataBase target, AppointmentDataBase source)
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
    /// Creates a generic appointment data object from AppointmentDataBase
    /// </summary>
    /// <param name="source">Source appointment data</param>
    /// <returns>Generic appointment data implementation</returns>
    public static AppointmentData ToAppointmentData(this AppointmentDataBase source)
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
/// Generic implementation of appointment data
/// Used for data conversion and manipulation
/// Inherits from AppointmentDataBase to eliminate property duplication
/// </summary>
public class AppointmentData : AppointmentDataBase
{
    // No properties needed - all inherited from AppointmentDataBase!
    // This completely eliminates property duplication
}