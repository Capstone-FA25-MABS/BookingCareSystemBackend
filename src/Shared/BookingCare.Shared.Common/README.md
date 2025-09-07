# Global Exception Handling Configuration Guide

This document provides guidance on configuring and using the global exception handling system in the BookingCare microservices project.

## Overview

The global exception handling system provides:
- Consistent error responses across all services
- Automatic logging of exceptions with correlation IDs
- Domain-specific exception types for healthcare scenarios
- Middleware and filter-based exception handling
- Base classes for controllers and services

## Quick Setup

### 1. For ASP.NET Core Web API Services

In your `Program.cs` or `Startup.cs`:

```csharp
using BookingCare.Shared.Common.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddGlobalExceptionHandling(); // Add this line

var app = builder.Build();

// Configure pipeline
app.UseGlobalExceptionHandling(); // Add this line - should be early in pipeline
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### 2. For Controllers

Inherit from the base controller:

```csharp
using BookingCare.Shared.Common.Controllers;

[Route("api/[controller]")]
public class UsersController : BaseApiController
{
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        // Your logic here
        var user = await _userService.GetByIdAsync(id);
        return Success(user); // Uses base controller methods
    }
}
```

### 3. For Services

Inherit from the base service:

```csharp
using BookingCare.Shared.Common.Services;

public class UserService : BaseService
{
    public UserService(ILogger<UserService> logger) : base(logger)
    {
    }

    public async Task<User> GetByIdAsync(Guid id)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            ValidateGuid(id, nameof(id));
            
            var user = await _repository.GetByIdAsync(id);
            if (user == null)
            {
                throw new UserExceptions.UserNotFoundException(id);
            }
            
            return user;
        }, "GetUserById");
    }
}
```

## Exception Types

### Base Exceptions

- `BookingCareException` - Base class for all domain exceptions
- `BusinessException` - For business logic violations
- `ValidationException` - For validation errors
- `NotFoundException` - For resource not found scenarios
- `UnauthorizedException` - For authentication failures
- `ForbiddenException` - For authorization failures
- `ConflictException` - For resource conflicts
- `ExternalServiceException` - For external service failures

### Domain-Specific Exceptions

#### Authentication & Authorization
```csharp
throw new AuthExceptions.InvalidCredentialsException();
throw new AuthExceptions.TokenExpiredException();
throw new AuthExceptions.AccountLockedException();
```

#### User Management
```csharp
throw new UserExceptions.UserNotFoundException(userId);
throw new UserExceptions.UserAlreadyExistsException(email);
throw new UserExceptions.ProfileIncompleteException(missingFields);
```

#### Appointments
```csharp
throw new AppointmentExceptions.AppointmentConflictException(appointmentTime, doctorId);
throw new AppointmentExceptions.InvalidAppointmentTimeException();
throw new AppointmentExceptions.PastAppointmentException("cancel");
```

#### Payment
```csharp
throw new PaymentExceptions.PaymentFailedException("Insufficient funds", transactionId);
throw new PaymentExceptions.InsufficientFundsException(required, available);
```

## Using Validation Helpers

```csharp
using BookingCare.Shared.Common.Helpers;

// Email validation
if (!ValidationHelper.IsValidEmail(email))
{
    throw new ValidationException(ValidationHelper.InvalidEmail("Email", email));
}

// Multiple validation errors
var errors = new List<ValidationError>();
if (string.IsNullOrEmpty(name))
    errors.Add(ValidationHelper.RequiredField("Name"));
if (age < 0 || age > 150)
    errors.Add(ValidationHelper.OutOfRange("Age", 0, 150, age));

if (errors.Any())
    throw new ValidationException("Validation failed", errors);
```

## Response Format

All exceptions are automatically converted to a consistent JSON response:

```json
{
  "success": false,
  "message": "User with identifier '123' was not found.",
  "data": null,
  "errors": [
    "User with identifier '123' was not found."
  ],
  "timestamp": "2024-01-15T10:30:00Z"
}
```

For validation exceptions:
```json
{
  "success": false,
  "message": "Validation failed",
  "data": null,
  "errors": [
    "Name: Name is required",
    "Email: Invalid email format"
  ],
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### Automatic DTO Validation

**🆕 NEW FEATURE**: Starting with the latest version, all ASP.NET Core model validation errors (including DTO validation attributes) are automatically converted to the consistent `ApiResponse<T>` format.

Previously, validation errors returned the default ASP.NET Core ProblemDetails format:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "request": ["The request field is required."],
    "$.clinicId": ["Invalid GUID format"]
  },
  "traceId": "00-abc123-def456-00"
}
```

Now they automatically return in the consistent format:
```json
{
  "success": false,
  "message": "One or more validation errors occurred.",
  "data": null,
  "errors": [
    "request: The request field is required.",
    "clinicId: Invalid GUID format"
  ],
  "timestamp": "2025-09-04T08:42:52.322026Z"
}
```

This works automatically with all standard validation attributes:
```csharp
public class CreateUserRequest
{
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Range(18, 120, ErrorMessage = "Age must be between 18 and 120")]
    public int Age { get; set; }
}
```

No additional configuration is required - this is automatically enabled when you call `AddGlobalExceptionHandling()`.

For detailed implementation information, see [CUSTOM_VALIDATION_FORMAT.md](./CUSTOM_VALIDATION_FORMAT.md).

## Configuration Examples

### Auth Service Example

```csharp
// Program.cs
builder.Services.AddGlobalExceptionHandling();

// AuthController.cs
public class AuthController : BaseApiController
{
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return Success(result, "Login successful");
    }
}

// AuthService.cs
public async Task<LoginResponse> LoginAsync(LoginRequest request)
{
    ValidateRequiredString(request.Email, nameof(request.Email));
    ValidateRequiredString(request.Password, nameof(request.Password));

    var user = await _userRepository.GetByEmailAsync(request.Email);
    if (user == null || !_passwordService.VerifyPassword(request.Password, user.PasswordHash))
    {
        throw new AuthExceptions.InvalidCredentialsException();
    }

    if (user.IsLocked)
    {
        throw new AuthExceptions.AccountLockedException();
    }

    // Generate token and return response
}
```

### User Service Example

```csharp
public class UserController : BaseApiController
{
    [HttpPost]
    public async Task<IActionResult> CreateUser(CreateUserRequest request)
    {
        var user = await _userService.CreateAsync(request);
        return Created(user);
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _userService.GetPagedAsync(pageNumber, pageSize);
        return Paginated(result);
    }
}
```

### Appointment Service Example

```csharp
public async Task<Appointment> BookAppointmentAsync(BookAppointmentRequest request)
{
    return await ExecuteWithErrorHandling(async () =>
    {
        // Validate appointment time
        if (!ValidationHelper.IsInFuture(request.AppointmentTime))
        {
            throw new AppointmentExceptions.InvalidAppointmentTimeException(
                "Appointment time must be in the future");
        }

        // Check for conflicts
        var hasConflict = await _repository.HasConflictAsync(request.DoctorId, request.AppointmentTime);
        if (hasConflict)
        {
            throw new AppointmentExceptions.AppointmentConflictException(
                request.AppointmentTime, request.DoctorId);
        }

        // Create appointment
        var appointment = new Appointment
        {
            UserId = request.UserId,
            DoctorId = request.DoctorId,
            AppointmentTime = request.AppointmentTime,
            Status = AppointmentStatus.Scheduled
        };

        return await _repository.CreateAsync(appointment);
    }, "BookAppointment", GetCorrelationId());
}
```

## Best Practices

1. **Use specific exception types** - Choose the most appropriate exception type for your scenario
2. **Include relevant details** - Add context information to the Details dictionary
3. **Log appropriately** - Use the base service logging methods with correlation IDs
4. **Validate early** - Use validation helpers to catch issues early
5. **Be consistent** - Use the base controller methods for consistent responses
6. **Handle external services** - Wrap external service calls with ExternalServiceException

## Logging

All exceptions are automatically logged with:
- User ID (if authenticated)
- Correlation ID
- Request path and method
- Full exception details (for internal server errors)

Example log output:
```
[Warning] Exception occurred - User: john.doe@example.com, Method: POST, Path: /api/users, CorrelationId: 12345, Exception: UserAlreadyExistsException
```

## Testing

You can test exception handling by throwing exceptions in your services:

```csharp
[HttpGet("test-error")]
public IActionResult TestError()
{
    throw new BusinessException("This is a test error");
}
```

The response will be automatically formatted and logged according to the configuration.
