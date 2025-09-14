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

    // Common patterns for bulk operations
    public const string UserPattern = "user:*";
    public const string DoctorPattern = "doctor:*";
    public const string ClinicPattern = "clinic:*";
    public const string AppointmentPattern = "appointment:*";
    public const string ServicePattern = "service:*";
    public const string AuthPattern = "auth:*";

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