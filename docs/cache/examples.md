# Redis Caching Examples for BookingCare System

This document provides practical examples of implementing Redis caching in different scenarios within the BookingCare system.

## Table of Contents

1. [Basic User Service Example](#basic-user-service-example)
2. [Doctor Service with Appointment Caching](#doctor-service-with-appointment-caching)
3. [Multi-level Caching for Clinic Data](#multi-level-caching-for-clinic-data)
4. [Session and Authentication Caching](#session-and-authentication-caching)
5. [Cache Warming Strategies](#cache-warming-strategies)
6. [Distributed Cache Invalidation](#distributed-cache-invalidation)
7. [Performance Monitoring](#performance-monitoring)

## Basic User Service Example

### User Model

```csharp
public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Address { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    
    [JsonIgnore]
    public string FullName => $"{FirstName} {LastName}";
}
```

### Cached User Service

```csharp
public class UserService
{
    private readonly ICacheService _cacheService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(
        ICacheService cacheService,
        IUserRepository userRepository,
        ILogger<UserService> logger)
    {
        _cacheService = cacheService;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        var cacheKey = CacheKeys.Format(CacheKeys.UserById, userId);
        
        return await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            _logger.LogInformation("Loading user {UserId} from database", userId);
            var user = await _userRepository.GetByIdAsync(userId);
            
            if (user != null)
            {
                _logger.LogInformation("User {UserId} loaded and cached", userId);
            }
            
            return user;
        }, TimeSpan.FromMinutes(30));
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        var cacheKey = CacheKeys.Format(CacheKeys.UserByEmail, email.ToLowerInvariant());
        
        return await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            var user = await _userRepository.GetByEmailAsync(email);
            
            if (user != null)
            {
                // Also cache by ID for consistency
                var userByIdKey = CacheKeys.Format(CacheKeys.UserById, user.Id);
                await _cacheService.SetAsync(userByIdKey, user, TimeSpan.FromMinutes(30));
            }
            
            return user;
        }, TimeSpan.FromMinutes(15)); // Shorter expiration for email-based lookup
    }

    public async Task<User> UpdateUserAsync(User user)
    {
        // Update in database first
        var updatedUser = await _userRepository.UpdateAsync(user);
        
        // Update cache with new data
        var userByIdKey = CacheKeys.Format(CacheKeys.UserById, user.Id);
        var userByEmailKey = CacheKeys.Format(CacheKeys.UserByEmail, user.Email.ToLowerInvariant());
        
        var cacheUpdateTasks = new[]
        {
            _cacheService.SetAsync(userByIdKey, updatedUser, TimeSpan.FromMinutes(30)),
            _cacheService.SetAsync(userByEmailKey, updatedUser, TimeSpan.FromMinutes(15))
        };
        
        await Task.WhenAll(cacheUpdateTasks);
        
        _logger.LogInformation("User {UserId} updated in database and cache", user.Id);
        return updatedUser;
    }

    public async Task DeleteUserAsync(int userId)
    {
        var user = await GetUserByIdAsync(userId);
        if (user == null) return;
        
        // Delete from database
        await _userRepository.DeleteAsync(userId);
        
        // Remove from cache
        await InvalidateUserCacheAsync(userId, user.Email);
        
        _logger.LogInformation("User {UserId} deleted from database and cache", userId);
    }

    private async Task InvalidateUserCacheAsync(int userId, string email)
    {
        var userByIdKey = CacheKeys.Format(CacheKeys.UserById, userId);
        var userByEmailKey = CacheKeys.Format(CacheKeys.UserByEmail, email.ToLowerInvariant());
        var userProfileKey = CacheKeys.Format(CacheKeys.UserProfile, userId);
        
        var invalidationTasks = new[]
        {
            _cacheService.RemoveAsync(userByIdKey),
            _cacheService.RemoveAsync(userByEmailKey),
            _cacheService.RemoveAsync(userProfileKey)
        };
        
        await Task.WhenAll(invalidationTasks);
    }
}
```

## Doctor Service with Appointment Caching

### Doctor and Appointment Models

```csharp
public class Doctor
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public int ClinicId { get; set; }
    public bool IsAvailable { get; set; }
}

public class Appointment
{
    public int Id { get; set; }
    public int DoctorId { get; set; }
    public int UserId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class DoctorSchedule
{
    public int DoctorId { get; set; }
    public DateTime Date { get; set; }
    public List<TimeSlot> AvailableSlots { get; set; } = new();
    public List<Appointment> Appointments { get; set; } = new();
}

public class TimeSlot
{
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsAvailable { get; set; }
}
```

### Doctor Service with Advanced Caching

```csharp
public class DoctorService
{
    private readonly ICacheService _cacheService;
    private readonly IDoctorRepository _doctorRepository;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly ILogger<DoctorService> _logger;

    public DoctorService(
        ICacheService cacheService,
        IDoctorRepository doctorRepository,
        IAppointmentRepository appointmentRepository,
        ILogger<DoctorService> logger)
    {
        _cacheService = cacheService;
        _doctorRepository = doctorRepository;
        _appointmentRepository = appointmentRepository;
        _logger = logger;
    }

    public async Task<Doctor?> GetDoctorByIdAsync(int doctorId)
    {
        var cacheKey = CacheKeys.Format(CacheKeys.DoctorById, doctorId);
        
        return await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            return await _doctorRepository.GetByIdAsync(doctorId);
        }, TimeSpan.FromMinutes(60)); // Doctors change less frequently
    }

    public async Task<List<Doctor>> GetDoctorsBySpecialtyAsync(string specialty)
    {
        var cacheKey = CacheKeys.Format(CacheKeys.DoctorBySpecialty, specialty.ToLowerInvariant());
        
        return await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            var doctors = await _doctorRepository.GetBySpecialtyAsync(specialty);
            
            // Also cache individual doctors
            var cacheTasks = doctors.Select(doctor =>
            {
                var doctorKey = CacheKeys.Format(CacheKeys.DoctorById, doctor.Id);
                return _cacheService.SetAsync(doctorKey, doctor, TimeSpan.FromMinutes(60));
            });
            
            await Task.WhenAll(cacheTasks);
            
            return doctors;
        }, TimeSpan.FromMinutes(30));
    }

    public async Task<DoctorSchedule?> GetDoctorScheduleAsync(int doctorId, DateTime date)
    {
        var cacheKey = CacheKeys.Format(CacheKeys.DoctorSchedule, doctorId, date.ToString("yyyy-MM-dd"));
        
        return await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            var appointments = await _appointmentRepository.GetByDoctorAndDateAsync(doctorId, date);
            var doctor = await GetDoctorByIdAsync(doctorId);
            
            if (doctor == null) return null;
            
            var schedule = new DoctorSchedule
            {
                DoctorId = doctorId,
                Date = date,
                Appointments = appointments,
                AvailableSlots = CalculateAvailableSlots(appointments, date)
            };
            
            return schedule;
        }, TimeSpan.FromMinutes(15)); // Schedule changes frequently
    }

    public async Task<Appointment> CreateAppointmentAsync(Appointment appointment)
    {
        // Create in database
        var createdAppointment = await _appointmentRepository.CreateAsync(appointment);
        
        // Invalidate related cache entries
        await InvalidateScheduleCacheAsync(appointment.DoctorId, appointment.AppointmentDate);
        await InvalidateUserAppointmentsCacheAsync(appointment.UserId);
        
        _logger.LogInformation(
            "Appointment {AppointmentId} created for doctor {DoctorId} on {Date}",
            createdAppointment.Id,
            appointment.DoctorId,
            appointment.AppointmentDate.ToString("yyyy-MM-dd"));
        
        return createdAppointment;
    }

    public async Task UpdateAppointmentAsync(Appointment appointment)
    {
        var existingAppointment = await _appointmentRepository.GetByIdAsync(appointment.Id);
        if (existingAppointment == null) return;
        
        // Update in database
        await _appointmentRepository.UpdateAsync(appointment);
        
        // Invalidate cache for both old and new dates/doctors if changed
        var invalidationTasks = new List<Task>
        {
            InvalidateScheduleCacheAsync(appointment.DoctorId, appointment.AppointmentDate),
            InvalidateUserAppointmentsCacheAsync(appointment.UserId)
        };
        
        // If doctor or date changed, invalidate old cache too
        if (existingAppointment.DoctorId != appointment.DoctorId)
        {
            invalidationTasks.Add(InvalidateScheduleCacheAsync(existingAppointment.DoctorId, existingAppointment.AppointmentDate));
        }
        
        if (existingAppointment.AppointmentDate.Date != appointment.AppointmentDate.Date)
        {
            invalidationTasks.Add(InvalidateScheduleCacheAsync(appointment.DoctorId, existingAppointment.AppointmentDate));
        }
        
        await Task.WhenAll(invalidationTasks);
    }

    private async Task InvalidateScheduleCacheAsync(int doctorId, DateTime date)
    {
        var scheduleKey = CacheKeys.Format(CacheKeys.DoctorSchedule, doctorId, date.ToString("yyyy-MM-dd"));
        await _cacheService.RemoveAsync(scheduleKey);
    }

    private async Task InvalidateUserAppointmentsCacheAsync(int userId)
    {
        var userAppointmentsKey = CacheKeys.Format(CacheKeys.UserAppointments, userId);
        await _cacheService.RemoveAsync(userAppointmentsKey);
    }

    private List<TimeSlot> CalculateAvailableSlots(List<Appointment> appointments, DateTime date)
    {
        // Simplified implementation
        var allSlots = GenerateTimeSlots(TimeSpan.FromHours(9), TimeSpan.FromHours(17), TimeSpan.FromMinutes(30));
        var bookedSlots = appointments.Select(a => new { a.StartTime, a.EndTime }).ToList();
        
        return allSlots.Where(slot => 
            !bookedSlots.Any(booked => 
                slot.StartTime >= booked.StartTime && slot.StartTime < booked.EndTime
            )
        ).ToList();
    }

    private List<TimeSlot> GenerateTimeSlots(TimeSpan startTime, TimeSpan endTime, TimeSpan duration)
    {
        var slots = new List<TimeSlot>();
        var current = startTime;
        
        while (current < endTime)
        {
            slots.Add(new TimeSlot
            {
                StartTime = current,
                EndTime = current.Add(duration),
                IsAvailable = true
            });
            current = current.Add(duration);
        }
        
        return slots;
    }
}
```

## Multi-level Caching for Clinic Data

### Clinic Models

```csharp
public class Clinic
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public List<string> Services { get; set; } = new();
    public int DoctorCount { get; set; }
    public decimal Rating { get; set; }
}

public class ClinicDetails
{
    public Clinic Clinic { get; set; } = new();
    public List<Doctor> Doctors { get; set; } = new();
    public List<MedicalService> Services { get; set; } = new();
    public List<Review> Reviews { get; set; } = new();
    public OperatingHours Hours { get; set; } = new();
}
```

### Multi-level Caching Strategy

```csharp
public class ClinicService
{
    private readonly ICacheService _cacheService;
    private readonly IClinicRepository _clinicRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly IServiceRepository _serviceRepository;
    private readonly ILogger<ClinicService> _logger;

    public ClinicService(
        ICacheService cacheService,
        IClinicRepository clinicRepository,
        IDoctorRepository doctorRepository,
        IServiceRepository serviceRepository,
        ILogger<ClinicService> logger)
    {
        _cacheService = cacheService;
        _clinicRepository = clinicRepository;
        _doctorRepository = doctorRepository;
        _serviceRepository = serviceRepository;
        _logger = logger;
    }

    // Level 1: Basic clinic data (long expiration)
    public async Task<Clinic?> GetClinicAsync(int clinicId)
    {
        var cacheKey = CacheKeys.Format(CacheKeys.ClinicById, clinicId);
        
        return await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            return await _clinicRepository.GetByIdAsync(clinicId);
        }, TimeSpan.FromHours(4)); // Long expiration for basic clinic data
    }

    // Level 2: Clinic with aggregated data (medium expiration)
    public async Task<ClinicDetails?> GetClinicDetailsAsync(int clinicId)
    {
        var cacheKey = $"clinic:details:{clinicId}";
        
        return await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            // Use cached clinic data if available
            var clinic = await GetClinicAsync(clinicId);
            if (clinic == null) return null;

            // Load additional data in parallel
            var doctorsTask = GetClinicDoctorsAsync(clinicId);
            var servicesTask = GetClinicServicesAsync(clinicId);
            var reviewsTask = GetClinicReviewsAsync(clinicId);
            var hoursTask = GetClinicHoursAsync(clinicId);

            await Task.WhenAll(doctorsTask, servicesTask, reviewsTask, hoursTask);

            return new ClinicDetails
            {
                Clinic = clinic,
                Doctors = doctorsTask.Result,
                Services = servicesTask.Result,
                Reviews = reviewsTask.Result,
                Hours = hoursTask.Result
            };
        }, TimeSpan.FromHours(1)); // Medium expiration for aggregated data
    }

    // Level 3: Frequently changing data (short expiration)
    public async Task<List<Doctor>> GetClinicDoctorsAsync(int clinicId)
    {
        var cacheKey = $"clinic:doctors:{clinicId}";
        
        return await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            var doctors = await _doctorRepository.GetByClinicIdAsync(clinicId);
            
            // Cache individual doctors too
            var doctorCacheTasks = doctors.Select(doctor =>
            {
                var doctorKey = CacheKeys.Format(CacheKeys.DoctorById, doctor.Id);
                return _cacheService.SetAsync(doctorKey, doctor, TimeSpan.FromMinutes(60));
            });
            
            await Task.WhenAll(doctorCacheTasks);
            
            return doctors;
        }, TimeSpan.FromMinutes(30));
    }

    // Geographic search with location-based caching
    public async Task<List<Clinic>> GetClinicsNearLocationAsync(double latitude, double longitude, int radiusKm)
    {
        // Create cache key based on rounded coordinates for better cache hits
        var roundedLat = Math.Round(latitude, 3);
        var roundedLng = Math.Round(longitude, 3);
        var cacheKey = $"clinic:location:{roundedLat}:{roundedLng}:{radiusKm}";
        
        return await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            var clinics = await _clinicRepository.GetByLocationAsync(latitude, longitude, radiusKm);
            
            // Cache individual clinics
            var clinicCacheTasks = clinics.Select(clinic =>
            {
                var clinicKey = CacheKeys.Format(CacheKeys.ClinicById, clinic.Id);
                return _cacheService.SetAsync(clinicKey, clinic, TimeSpan.FromHours(4));
            });
            
            await Task.WhenAll(clinicCacheTasks);
            
            return clinics;
        }, TimeSpan.FromMinutes(20)); // Geographic data changes moderately
    }

    // Bulk cache warming for popular clinics
    public async Task WarmPopularClinicsAsync()
    {
        _logger.LogInformation("Starting cache warming for popular clinics");
        
        var popularClinicIds = await _clinicRepository.GetPopularClinicIdsAsync();
        
        var warmingTasks = popularClinicIds.Select(async clinicId =>
        {
            try
            {
                await GetClinicDetailsAsync(clinicId);
                _logger.LogDebug("Warmed cache for clinic {ClinicId}", clinicId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to warm cache for clinic {ClinicId}", clinicId);
            }
        });
        
        await Task.WhenAll(warmingTasks);
        
        _logger.LogInformation("Cache warming completed for {Count} popular clinics", popularClinicIds.Count);
    }

    private async Task<List<MedicalService>> GetClinicServicesAsync(int clinicId)
    {
        var cacheKey = $"clinic:services:{clinicId}";
        
        return await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            return await _serviceRepository.GetByClinicIdAsync(clinicId);
        }, TimeSpan.FromHours(2));
    }

    private async Task<List<Review>> GetClinicReviewsAsync(int clinicId)
    {
        var cacheKey = $"clinic:reviews:{clinicId}";
        
        return await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            return await _clinicRepository.GetReviewsAsync(clinicId);
        }, TimeSpan.FromMinutes(30)); // Reviews change more frequently
    }

    private async Task<OperatingHours> GetClinicHoursAsync(int clinicId)
    {
        var cacheKey = $"clinic:hours:{clinicId}";
        
        return await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            return await _clinicRepository.GetOperatingHoursAsync(clinicId);
        }, TimeSpan.FromHours(6));
    }
}
```

## Session and Authentication Caching

### Authentication Models

```csharp
public class UserSession
{
    public string SessionId { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime LastAccessedAt { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class TokenData
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int UserId { get; set; }
    public List<string> Permissions { get; set; } = new();
}
```

### Authentication Cache Service

```csharp
public class AuthenticationCacheService
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<AuthenticationCacheService> _logger;

    public AuthenticationCacheService(ICacheService cacheService, ILogger<AuthenticationCacheService> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    // Session management
    public async Task CacheSessionAsync(UserSession session)
    {
        var sessionKey = CacheKeys.Format(CacheKeys.AuthToken, session.SessionId);
        var userSessionsKey = CacheKeys.Format(CacheKeys.UserSessions, session.UserId);
        
        // Cache individual session
        await _cacheService.SetAsync(sessionKey, session, TimeSpan.FromMinutes(30));
        
        // Update user's active sessions list
        var userSessions = await _cacheService.GetAsync<List<string>>(userSessionsKey) ?? new List<string>();
        if (!userSessions.Contains(session.SessionId))
        {
            userSessions.Add(session.SessionId);
            await _cacheService.SetAsync(userSessionsKey, userSessions, TimeSpan.FromHours(24));
        }
        
        _logger.LogDebug("Session {SessionId} cached for user {UserId}", session.SessionId, session.UserId);
    }

    public async Task<UserSession?> GetSessionAsync(string sessionId)
    {
        var sessionKey = CacheKeys.Format(CacheKeys.AuthToken, sessionId);
        var session = await _cacheService.GetAsync<UserSession>(sessionKey);
        
        if (session != null)
        {
            // Update last accessed time
            session.LastAccessedAt = DateTime.UtcNow;
            await _cacheService.SetAsync(sessionKey, session, TimeSpan.FromMinutes(30));
        }
        
        return session;
    }

    public async Task InvalidateSessionAsync(string sessionId)
    {
        var sessionKey = CacheKeys.Format(CacheKeys.AuthToken, sessionId);
        var session = await _cacheService.GetAsync<UserSession>(sessionKey);
        
        if (session != null)
        {
            // Remove from individual session cache
            await _cacheService.RemoveAsync(sessionKey);
            
            // Remove from user's sessions list
            var userSessionsKey = CacheKeys.Format(CacheKeys.UserSessions, session.UserId);
            var userSessions = await _cacheService.GetAsync<List<string>>(userSessionsKey);
            
            if (userSessions != null && userSessions.Contains(sessionId))
            {
                userSessions.Remove(sessionId);
                await _cacheService.SetAsync(userSessionsKey, userSessions, TimeSpan.FromHours(24));
            }
            
            _logger.LogInformation("Session {SessionId} invalidated for user {UserId}", sessionId, session.UserId);
        }
    }

    public async Task InvalidateAllUserSessionsAsync(int userId)
    {
        var userSessionsKey = CacheKeys.Format(CacheKeys.UserSessions, userId);
        var userSessions = await _cacheService.GetAsync<List<string>>(userSessionsKey);
        
        if (userSessions != null)
        {
            var invalidationTasks = userSessions.Select(sessionId =>
            {
                var sessionKey = CacheKeys.Format(CacheKeys.AuthToken, sessionId);
                return _cacheService.RemoveAsync(sessionKey);
            }).ToList();
            
            // Remove user sessions list
            invalidationTasks.Add(_cacheService.RemoveAsync(userSessionsKey));
            
            await Task.WhenAll(invalidationTasks);
            
            _logger.LogInformation("All sessions invalidated for user {UserId} ({Count} sessions)", userId, userSessions.Count);
        }
    }

    // Token management
    public async Task CacheTokenDataAsync(string tokenId, TokenData tokenData)
    {
        var tokenKey = CacheKeys.Format(CacheKeys.AuthToken, tokenId);
        var refreshTokenKey = CacheKeys.Format(CacheKeys.RefreshToken, tokenData.RefreshToken);
        
        var timeUntilExpiration = tokenData.ExpiresAt - DateTime.UtcNow;
        if (timeUntilExpiration > TimeSpan.Zero)
        {
            await Task.WhenAll(
                _cacheService.SetAsync(tokenKey, tokenData, timeUntilExpiration),
                _cacheService.SetAsync(refreshTokenKey, tokenData, timeUntilExpiration.Add(TimeSpan.FromDays(7))) // Longer for refresh
            );
        }
    }

    public async Task<TokenData?> GetTokenDataAsync(string tokenId)
    {
        var tokenKey = CacheKeys.Format(CacheKeys.AuthToken, tokenId);
        return await _cacheService.GetAsync<TokenData>(tokenKey);
    }

    public async Task<TokenData?> GetTokenByRefreshTokenAsync(string refreshToken)
    {
        var refreshTokenKey = CacheKeys.Format(CacheKeys.RefreshToken, refreshToken);
        return await _cacheService.GetAsync<TokenData>(refreshTokenKey);
    }

    // Permission caching with hierarchical structure
    public async Task CacheUserPermissionsAsync(int userId, List<string> permissions)
    {
        var permissionsKey = $"auth:permissions:{userId}";
        await _cacheService.SetAsync(permissionsKey, permissions, TimeSpan.FromMinutes(15));
    }

    public async Task<List<string>?> GetUserPermissionsAsync(int userId)
    {
        var permissionsKey = $"auth:permissions:{userId}";
        return await _cacheService.GetAsync<List<string>>(permissionsKey);
    }

    // Rate limiting cache
    public async Task<bool> CheckRateLimitAsync(string identifier, int maxRequests, TimeSpan window)
    {
        var rateLimitKey = $"ratelimit:{identifier}";
        var currentCount = await _cacheService.GetAsync<int?>(rateLimitKey) ?? 0;
        
        if (currentCount >= maxRequests)
        {
            return false; // Rate limit exceeded
        }
        
        // Increment counter
        await _cacheService.SetAsync(rateLimitKey, currentCount + 1, window);
        return true; // Within rate limit
    }

    // Failed login attempts tracking
    public async Task TrackFailedLoginAsync(string identifier)
    {
        var failedAttemptsKey = $"auth:failed:{identifier}";
        var attempts = await _cacheService.GetAsync<int?>(failedAttemptsKey) ?? 0;
        
        await _cacheService.SetAsync(failedAttemptsKey, attempts + 1, TimeSpan.FromMinutes(15));
        
        if (attempts + 1 >= 5) // Lock after 5 failed attempts
        {
            var lockKey = $"auth:locked:{identifier}";
            await _cacheService.SetAsync(lockKey, true, TimeSpan.FromMinutes(30));
            
            _logger.LogWarning("Account locked due to failed login attempts: {Identifier}", identifier);
        }
    }

    public async Task<bool> IsAccountLockedAsync(string identifier)
    {
        var lockKey = $"auth:locked:{identifier}";
        return await _cacheService.ExistsAsync(lockKey);
    }

    public async Task ClearFailedLoginAttemptsAsync(string identifier)
    {
        var failedAttemptsKey = $"auth:failed:{identifier}";
        var lockKey = $"auth:locked:{identifier}";
        
        await Task.WhenAll(
            _cacheService.RemoveAsync(failedAttemptsKey),
            _cacheService.RemoveAsync(lockKey)
        );
    }
}
```

This comprehensive example shows how to implement caching across different layers of your application, from basic entity caching to complex multi-level strategies and authentication systems. Each pattern addresses different use cases and performance requirements in the BookingCare system.