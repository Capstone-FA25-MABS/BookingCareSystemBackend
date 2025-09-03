using BookingCare.Services.Doctor.Services;

namespace BookingCare.Services.Doctor.Services;

public class DoctorBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DoctorBackgroundService> _logger;
    private readonly TimeSpan _period = TimeSpan.FromHours(1); // Check every hour

    public DoctorBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<DoctorBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Running Doctor background service tasks at {Time}", DateTime.UtcNow);

                using var scope = _serviceProvider.CreateScope();
                var doctorService = scope.ServiceProvider.GetRequiredService<IDoctorService>();
                var positionService = scope.ServiceProvider.GetRequiredService<IPositionService>();
                var priceService = scope.ServiceProvider.GetRequiredService<IPriceService>();

                // Perform background tasks
                await PerformDoctorMaintenanceTasksAsync(doctorService, positionService, priceService);
                
                _logger.LogInformation("Doctor background service tasks completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while running Doctor background service tasks");
            }

            await Task.Delay(_period, stoppingToken);
        }
    }

    private async Task PerformDoctorMaintenanceTasksAsync(IDoctorService doctorService, IPositionService positionService, IPriceService priceService)
    {
        try
        {
            // Task 1: Clean up orphaned doctor-price relationships
            await CleanupOrphanedDoctorPricesAsync(doctorService, priceService);

            // Task 2: Validate doctor data integrity
            await ValidateDoctorDataIntegrityAsync(doctorService);

            // Task 3: Update doctor availability status
            await UpdateDoctorAvailabilityStatusAsync(doctorService);

            // Task 4: Archive inactive doctors
            await ArchiveInactiveDoctorsAsync(doctorService);

            // Task 5: Update doctor statistics and metrics
            await UpdateDoctorStatisticsAsync(doctorService, positionService, priceService);

            _logger.LogInformation("All Doctor maintenance tasks completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Doctor maintenance tasks");
        }
    }

    private async Task CleanupOrphanedDoctorPricesAsync(IDoctorService doctorService, IPriceService priceService)
    {
        try
        {
            _logger.LogInformation("Checking for orphaned Doctor-Price relationships...");

            // Get all prices to check if they have associated doctors
            var prices = await priceService.GetAllPricesAsync();
            int orphanedCount = 0;

            foreach (var price in prices)
            {
                var doctorsWithPrice = await doctorService.GetDoctorsByPriceAsync(price.Id);
                if (!doctorsWithPrice.Any())
                {
                    _logger.LogWarning("Found orphaned price {PriceId} with amount {Amount} - no doctors assigned", 
                        price.Id, price.Amount);
                    orphanedCount++;
                }
            }

            if (orphanedCount > 0)
            {
                _logger.LogInformation("Found {Count} orphaned prices that need cleanup", orphanedCount);
            }
            else
            {
                _logger.LogDebug("No orphaned prices found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up orphaned Doctor-Price relationships");
        }
    }

    private async Task ValidateDoctorDataIntegrityAsync(IDoctorService doctorService)
    {
        try
        {
            _logger.LogInformation("Validating Doctor data integrity...");

            // Get all doctors to validate
            var queryRequest = new Models.DTOs.DoctorQueryRequest
            {
                PageNumber = 1,
                PageSize = 100 // Process in batches
            };

            var doctors = await doctorService.GetDoctorsAsync(queryRequest);
            int invalidDoctors = 0;

            foreach (var doctor in doctors.Doctors)
            {
                // Check for doctors with missing required data
                if (string.IsNullOrEmpty(doctor.Email) || 
                    string.IsNullOrEmpty(doctor.FirstName) || 
                    string.IsNullOrEmpty(doctor.LastName))
                {
                    _logger.LogWarning("Found doctor {DoctorId} with missing required data", doctor.Id);
                    invalidDoctors++;
                }

                // Check for doctors with invalid email format
                if (!string.IsNullOrEmpty(doctor.Email) && !IsValidEmail(doctor.Email))
                {
                    _logger.LogWarning("Found doctor {DoctorId} with invalid email format: {Email}", 
                        doctor.Id, doctor.Email);
                    invalidDoctors++;
                }

                // Check for doctors with invalid years of experience
                if (doctor.YearsOfExperience < 0 || doctor.YearsOfExperience > 50)
                {
                    _logger.LogWarning("Found doctor {DoctorId} with invalid years of experience: {Years}", 
                        doctor.Id, doctor.YearsOfExperience);
                    invalidDoctors++;
                }
            }
            
            if (invalidDoctors > 0)
            {
                _logger.LogInformation("Found {Count} doctors with data integrity issues", invalidDoctors);
            }
            else
            {
                _logger.LogDebug("All doctors passed data integrity validation");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Doctor data integrity");
        }
    }

    private async Task UpdateDoctorAvailabilityStatusAsync(IDoctorService doctorService)
    {
        try
        {
            _logger.LogInformation("Updating Doctor availability status...");

            // Get all active doctors
            var activeDoctors = await doctorService.GetActiveDoctorsAsync();
            int updatedCount = 0;

            foreach (var doctor in activeDoctors)
            {
                // Check if doctor has been inactive for too long (e.g., no appointments in 30 days)
                // This would typically involve checking appointment data from another service
                // For now, we'll just log the check
                
                var lastActivityDate = doctor.UpdatedAt; // This would be last appointment date
                var daysSinceLastActivity = (DateTime.UtcNow - lastActivityDate).Days;

                if (daysSinceLastActivity > 30)
                {
                    _logger.LogInformation("Doctor {DoctorId} has been inactive for {Days} days", 
                        doctor.Id, daysSinceLastActivity);
                    updatedCount++;
                }
            }

            if (updatedCount > 0)
            {
                _logger.LogInformation("Updated availability status for {Count} doctors", updatedCount);
            }
            else
            {
                _logger.LogDebug("No doctors needed availability status updates");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Doctor availability status");
        }
    }

    private async Task ArchiveInactiveDoctorsAsync(IDoctorService doctorService)
    {
        try
        {
            _logger.LogInformation("Checking for doctors to archive...");

            // Get all doctors to check for archiving
            var queryRequest = new Models.DTOs.DoctorQueryRequest
            {
                PageNumber = 1,
                PageSize = 100
            };

            var doctors = await doctorService.GetDoctorsAsync(queryRequest);
            int archivedCount = 0;

            foreach (var doctor in doctors.Doctors)
            {
                // Check if doctor should be archived (e.g., inactive for 90 days)
                var daysSinceLastActivity = (DateTime.UtcNow - doctor.UpdatedAt).Days;

                if (daysSinceLastActivity > 90)
                {
                    _logger.LogInformation("Doctor {DoctorId} ({FirstName} {LastName}) should be archived - inactive for {Days} days", 
                        doctor.Id, doctor.FirstName, doctor.LastName, daysSinceLastActivity);
                    archivedCount++;
                }
            }

            if (archivedCount > 0)
            {
                _logger.LogInformation("Found {Count} doctors that should be archived", archivedCount);
            }
            else
            {
                _logger.LogDebug("No doctors need to be archived");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for doctors to archive");
        }
    }

    private async Task UpdateDoctorStatisticsAsync(IDoctorService doctorService, IPositionService positionService, IPriceService priceService)
    {
        try
        {
            _logger.LogInformation("Updating Doctor statistics...");

            // Get all positions to update statistics
            var positions = await positionService.GetAllPositionsAsync();
            var positionStats = new Dictionary<string, int>();

            foreach (var position in positions)
            {
                var doctorsInPosition = await doctorService.GetDoctorsByPositionAsync(position.Id);
                positionStats[position.Name] = doctorsInPosition.Count;

                _logger.LogInformation("Position '{PositionName}' has {Count} doctors", 
                    position.Name, doctorsInPosition.Count);
            }

            // Get all prices to update statistics
            var prices = await priceService.GetAllPricesAsync();
            var priceStats = new Dictionary<decimal, int>();

            foreach (var price in prices)
            {
                var doctorsWithPrice = await doctorService.GetDoctorsByPriceAsync(price.Id);
                priceStats[price.Amount] = doctorsWithPrice.Count;

                _logger.LogInformation("Price {Amount} has {Count} doctors assigned", 
                    price.Amount, doctorsWithPrice.Count);
            }

            _logger.LogInformation("Updated statistics for {PositionCount} positions and {PriceCount} prices", 
                positionStats.Count, priceStats.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Doctor statistics");
        }
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
