# 📊 BookingCare Common Metrics Usage Guide

## 🎯 Overview

BookingCare Common Metrics provides a unified way to collect metrics across all microservices using Prometheus. This guide shows how to integrate metrics into your service.

## 🚀 Quick Setup

### 1. Add to Program.cs

```csharp
using BookingCare.Shared.Common.Extensions;
using BookingCare.Shared.Common.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add BookingCare metrics
builder.Services.AddBookingCareMetrics("auth-service"); // Replace with your service name

var app = builder.Build();

// Enable metrics if configured
var enableMetrics = Environment.GetEnvironmentVariable("ENABLE_PROMETHEUS_METRICS") == "true";
app.UseBookingCareMetrics(enableMetrics);

app.Run();
```

### 2. Add to Startup.cs (Legacy)

```csharp
using BookingCare.Shared.Common.Extensions;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Add BookingCare metrics
        services.AddBookingCareMetrics("user-service");
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Enable metrics
        var enableMetrics = PrometheusMetricsExtensions.IsMetricsEnabled();
        app.UseBookingCareMetrics(enableMetrics);
    }
}
```

## 📈 Using Metrics in Controllers

```csharp
using BookingCare.Shared.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IBookingCareMetrics _metrics;

    public AuthController(IAuthService authService, IBookingCareMetrics metrics)
    {
        _authService = authService;
        _metrics = metrics;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);
            
            // Record successful login
            _metrics.RecordAuthMetric("login", "success");
            _metrics.SetActiveItems("logged_in_users", await GetActiveUserCount());
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            // Record failed login
            _metrics.RecordAuthMetric("login", "failure");
            _metrics.IncrementError("AuthenticationException", "login");
            throw;
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        
        // Record user registration
        _metrics.RecordUserMetric("registration", "new_user");
        _metrics.RecordBusinessMetric("total_users", await GetTotalUserCount());
        
        return Ok(result);
    }
}
```

## 🗃️ Database Operations with Metrics

```csharp
using BookingCare.Shared.Common.Helpers;
using BookingCare.Shared.Common.Interfaces;

public class UserRepository
{
    private readonly IBookingCareMetrics _metrics;
    private readonly DbContext _context;

    public UserRepository(IBookingCareMetrics metrics, DbContext context)
    {
        _metrics = metrics;
        _context = context;
    }

    public async Task<User> GetByIdAsync(int id)
    {
        return await DatabaseMetricsHelper.ExecuteWithMetricsAsync(
            _metrics,
            "select",
            "users",
            async () => await _context.Users.FindAsync(id)
        );
    }

    public async Task<User> CreateUserAsync(User user)
    {
        return await DatabaseMetricsHelper.ExecuteWithMetricsAsync(
            _metrics,
            "insert", 
            "users",
            async () =>
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                return user;
            }
        );
    }
}
```

## 🔗 gRPC Calls with Metrics

```csharp
using BookingCare.Shared.Common.Helpers;

public class AppointmentService
{
    private readonly IBookingCareMetrics _metrics;
    private readonly UserService.UserServiceClient _userClient;

    public async Task<AppointmentDto> CreateAppointmentAsync(CreateAppointmentRequest request)
    {
        // Get user details via gRPC with metrics
        var user = await GrpcMetricsHelper.ExecuteGrpcCallAsync(
            _metrics,
            "GetUser",
            async () => await _userClient.GetUserAsync(new GetUserRequest { UserId = request.UserId })
        );

        // Record appointment creation
        _metrics.RecordAppointmentMetric("created", "success");
        _metrics.SetActiveItems("pending_appointments", await GetPendingAppointmentCount());

        return new AppointmentDto();
    }
}
```

## 🌐 External API Calls with Metrics

```csharp
using BookingCare.Shared.Common.Helpers;

public class PaymentService
{
    private readonly IBookingCareMetrics _metrics;
    private readonly HttpClient _httpClient;

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        var result = await ExternalApiMetricsHelper.ExecuteHttpCallAsync(
            _metrics,
            "stripe",
            "/v1/charges",
            async () => await _httpClient.PostAsync("/v1/charges", GetPaymentContent(request)),
            async response => 
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PaymentResult>(content);
            }
        );

        // Record payment metrics
        _metrics.RecordPaymentMetric("charge", result.Success ? "success" : "failed", request.Amount);
        
        return result;
    }
}
```

## 🏥 Service-Specific Business Metrics

### Auth Service Metrics
```csharp
// Login tracking
_metrics.RecordAuthMetric("login", "success");
_metrics.RecordAuthMetric("login", "failure");
_metrics.RecordAuthMetric("logout", "success");
_metrics.RecordAuthMetric("token_refresh", "success");

// Active sessions
_metrics.SetActiveItems("active_sessions", sessionCount);
_metrics.SetActiveItems("logged_in_users", userCount);
```

### Appointment Service Metrics
```csharp
// Appointment lifecycle
_metrics.RecordAppointmentMetric("created", "success");
_metrics.RecordAppointmentMetric("confirmed", "success");
_metrics.RecordAppointmentMetric("cancelled", "patient_request");
_metrics.RecordAppointmentMetric("cancelled", "doctor_unavailable");

// Appointment status tracking
_metrics.SetActiveItems("pending_appointments", pendingCount);
_metrics.SetActiveItems("confirmed_appointments", confirmedCount);
```

### Payment Service Metrics
```csharp
// Payment operations
_metrics.RecordPaymentMetric("charge", "success", 100.50);
_metrics.RecordPaymentMetric("refund", "success", 50.25);
_metrics.RecordPaymentMetric("charge", "failed", 200.00);

// Revenue tracking
_metrics.RecordBusinessMetric("daily_revenue", todayRevenue, ("period", "daily"));
_metrics.RecordBusinessMetric("monthly_revenue", monthRevenue, ("period", "monthly"));
```

### User Service Metrics
```csharp
// User operations
_metrics.RecordUserMetric("registration", "patient");
_metrics.RecordUserMetric("registration", "doctor");
_metrics.RecordUserMetric("profile_update", "success");

// User statistics
_metrics.SetActiveItems("total_patients", patientCount);
_metrics.SetActiveItems("active_doctors", activeDoctorCount);
```

## 📊 Available Metrics

### System Metrics (Automatic)
- `http_requests_total` - HTTP request counter
- `http_request_duration_seconds` - HTTP request duration
- `process_cpu_seconds_total` - CPU usage
- `dotnet_collection_count_total` - GC collections

### BookingCare Business Metrics
- `bookingcare_operations_total` - Service operations
- `bookingcare_errors_total` - Error tracking
- `bookingcare_operation_duration_seconds` - Operation timing
- `bookingcare_database_duration_ms` - Database performance
- `bookingcare_grpc_duration_ms` - gRPC call timing
- `bookingcare_external_api_duration_ms` - External API timing
- `bookingcare_active_items` - Current state gauges
- `bookingcare_business_events_total` - Business event counters
- `bookingcare_business_metrics` - Business value gauges

## 🔧 Environment Variables

```bash
# Enable metrics collection
ENABLE_PROMETHEUS_METRICS=true
SERVICE_NAME=auth-service
METRICS_PATH=/metrics
```

## ✅ Verification

Test metrics endpoint:
```bash
curl http://localhost:6003/metrics
```

Expected output:
```
# HELP bookingcare_operations_total Total operations performed
# TYPE bookingcare_operations_total counter
bookingcare_operations_total{service="auth-service",operation="login",result="success"} 45

# HELP bookingcare_active_items Current number of active items
# TYPE bookingcare_active_items gauge  
bookingcare_active_items{service="auth-service",item_type="logged_in_users"} 12
```