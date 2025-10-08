using BookingCare.Services.Appointment.Data;

namespace BookingCare.Services.Appointment.Services;

/// <summary>
/// Service for initializing default data
/// </summary>
public class DataInitializationService
{
    private readonly AppointmentDbContext _context;
    private readonly ILogger<DataInitializationService> _logger;

    public DataInitializationService(
        AppointmentDbContext context,
        ILogger<DataInitializationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Initialize default data for appointment service
    /// </summary>
    public async Task InitializeDefaultDataAsync()
    {
        try
        {
            // Ensure database is created
            await _context.Database.EnsureCreatedAsync();

            _logger.LogInformation("Default data initialization completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing default data for Appointment service");
            throw new InvalidOperationException("Failed to initialize default data for Appointment service. Please check database connection and configuration.", ex);
        }
    }

}
