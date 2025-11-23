namespace BookingCare.Shared.Cache.Constants;

/// <summary>
/// Cache key constants for different entities and operations
/// </summary>
public static class CacheKeys
{
    // User related cache keys
    public const string UserById = "user:id:{0}";
    public const string UserByEmail = "user:email:{0}";
    public const string UserProfile = "user:profile:{0}";

    // Doctor related cache keys
    public const string DoctorById = "doctor:id:{0}";
    public const string DoctorBySpecialty = "doctor:specialty:{0}";
    public const string DoctorSchedule = "doctor:schedule:{0}:{1}"; // doctorId:date
    public const string DoctorList = "doctor:list:{0}:{1}"; // page:size

    // Clinic related cache keys
    public const string ClinicById = "clinic:id:{0}";
    public const string ClinicByLocation = "clinic:location:{0}";
    public const string ClinicList = "clinic:list:{0}:{1}"; // page:size

    // Appointment related cache keys
    public const string AppointmentById = "appointment:id:{0}";
    public const string UserAppointments = "appointment:user:{0}";
    public const string DoctorAppointments = "appointment:doctor:{0}:{1}"; // doctorId:date

    // Medical service related cache keys
    public const string ServiceById = "service:id:{0}";
    public const string ServiceByCategory = "service:category:{0}";
    public const string ServiceList = "service:list:{0}:{1}"; // page:size

    // Authentication related cache keys
    public const string AuthToken = "auth:token:{0}";
    public const string RefreshToken = "auth:refresh:{0}";
    public const string UserSessions = "auth:sessions:{0}";

    // Configuration cache keys
    public const string SystemConfig = "config:system";
    public const string AppSettings = "config:appsettings";

    // Analytics cache keys
    public const string DailyStats = "analytics:daily:{0}"; // date
    public const string MonthlyStats = "analytics:monthly:{0}"; // month

    // Notification related cache keys
    public const string OtpByKey = "otp:{0}";
    public const string OtpPurposeEmail = "otp:purpose:{0}:email:{1}"; // purpose:email
    public const string OtpPurposePhone = "otp:purpose:{0}:phone:{1}"; // purpose:phone
    public const string OtpVerified = "otp:verified:{0}:{1}"; // purpose:subject
    public const string OtpPattern = "otp:*";

    // Schedule related cache keys
    public const string SchedulePatternById = "schedule_pattern:id:{0}";
    public const string AllSchedulePatterns = "schedule_patterns:all";
    public const string AppointmentTimeById = "appointment_time:id:{0}";
    public const string AllAppointmentTimes = "appointment_times:all";
    public const string DoctorDailySchedule = "doctor_schedule:daily:{0}:{1}"; // doctorId:date
    public const string DoctorScheduleRange = "doctor_schedule:range:{0}:{1}:{2}"; // doctorId:startDate:endDate
    public const string DoctorExceptions = "doctor_exceptions:{0}:{1}"; // doctorId:date
    public const string AvailableSlots = "available_slots:{0}:{1}:{2}"; // doctorId:date:serviceId
    public const string ServiceSchedules = "service_schedules:{0}"; // serviceId
    public const string ClinicExceptions = "clinic_exceptions:{0}:{1}"; // clinicId:date

    // Service Medical Schedule related cache keys
    public const string ServiceMedicalDailySchedule = "service_medical_schedule:daily:{0}:{1}"; // serviceMedicalId:date
    public const string ServiceMedicalScheduleRange = "service_medical_schedule:range:{0}:{1}:{2}"; // serviceMedicalId:startDate:endDate
    public const string ServiceMedicalExceptions = "service_medical_exceptions:{0}:{1}"; // serviceMedicalId:date
    public const string ServiceMedicalAvailableSlots = "service_medical_available_slots:{0}:{1}"; // serviceMedicalId:date

    // Common patterns for bulk operations
    public const string UserPattern = "user:*";
    public const string DoctorPattern = "doctor:*";
    public const string ClinicPattern = "clinic:*";
    public const string AppointmentPattern = "appointment:*";
    public const string ServicePattern = "service:*";
    public const string AuthPattern = "auth:*";
    public const string SchedulePattern = "schedule*";

    // Cache expiration times in minutes
    public const int ShortCacheExpiration = 5;
    public const int MediumCacheExpiration = 30;
    public const int LongCacheExpiration = 120;

    /// <summary>
    /// Format cache key with parameters
    /// </summary>
    /// <param name="template">Cache key template</param>
    /// <param name="args">Arguments to format</param>
    /// <returns>Formatted cache key</returns>
    public static string Format(string template, params object[] args)
    {
        return string.Format(template, args);
    }
}