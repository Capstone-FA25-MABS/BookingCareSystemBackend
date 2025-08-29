# Integration Steps for BookingCare Auth Service

Follow these steps to integrate the global exception handling system into your Auth service:

## Step 1: Add Project Reference

Add the following to `BookingCare.Services.Auth.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="..\..\Shared\BookingCare.Shared.Common\BookingCare.Shared.Common.csproj" />
</ItemGroup>
```

## Step 2: Update Program.cs

Replace your current `Program.cs` with:

```csharp
using BookingCare.Services.Auth.Services;
using BookingCare.Shared.Common.Extensions; // Add this
using Microsoft.AspNetCore.Server.Kestrel.Core;

// Enable HTTP/2 without TLS for gRPC (development only)
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(6003, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });
    options.ListenAnyIP(6013, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add global exception handling
builder.Services.AddGlobalExceptionHandling();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Add global exception handling middleware (early in pipeline)
app.UseGlobalExceptionHandling();

app.UseRouting();
app.MapControllers();

// Configure gRPC
app.MapGrpcService<GreeterService>();
app.MapGet("/", () => "BookingCare Auth Service is running...");

app.Run();
```

## Step 3: Create a Sample Controller

Create `Controllers/AuthController.cs`:

```csharp
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Exceptions.Domain;
using BookingCare.Shared.Common.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Auth.Controllers;

public class AuthController : BaseApiController
{
    private readonly ILogger<AuthController> _logger;

    public AuthController(ILogger<AuthController> logger)
    {
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // Validate input
        if (string.IsNullOrEmpty(request.Email))
        {
            throw new ValidationException(ValidationHelper.RequiredField("Email"));
        }

        if (!ValidationHelper.IsValidEmail(request.Email))
        {
            throw new ValidationException(ValidationHelper.InvalidEmail("Email", request.Email));
        }

        if (string.IsNullOrEmpty(request.Password))
        {
            throw new ValidationException(ValidationHelper.RequiredField("Password"));
        }

        // Simulate authentication logic
        if (request.Email == "invalid@test.com")
        {
            throw new AuthExceptions.InvalidCredentialsException();
        }

        if (request.Email == "locked@test.com")
        {
            throw new AuthExceptions.AccountLockedException();
        }

        // Success case
        var response = new LoginResponse
        {
            Token = "sample-jwt-token",
            ExpiresIn = 3600,
            User = new UserInfo
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                Name = "Test User"
            }
        };

        return Success(response, "Login successful");
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var validationErrors = new List<ValidationError>();

        if (string.IsNullOrEmpty(request.Email))
            validationErrors.Add(ValidationHelper.RequiredField("Email"));
        else if (!ValidationHelper.IsValidEmail(request.Email))
            validationErrors.Add(ValidationHelper.InvalidEmail("Email", request.Email));

        if (string.IsNullOrEmpty(request.Password))
            validationErrors.Add(ValidationHelper.RequiredField("Password"));
        else if (request.Password.Length < 8)
            validationErrors.Add(ValidationHelper.TooShort("Password", 8, request.Password));

        if (string.IsNullOrEmpty(request.Name))
            validationErrors.Add(ValidationHelper.RequiredField("Name"));

        if (validationErrors.Any())
            throw new ValidationException("Registration validation failed", validationErrors);

        // Simulate user already exists
        if (request.Email == "existing@test.com")
        {
            throw new UserExceptions.UserAlreadyExistsException(request.Email);
        }

        var response = new RegisterResponse
        {
            UserId = Guid.NewGuid(),
            Message = "Registration successful"
        };

        return Created(response);
    }

    [HttpGet("test-errors")]
    public IActionResult TestErrors([FromQuery] string errorType = "business")
    {
        return errorType.ToLower() switch
        {
            "business" => throw new BusinessException("This is a test business error"),
            "validation" => throw new ValidationException("This is a test validation error"),
            "notfound" => throw new NotFoundException("TestResource", "123"),
            "unauthorized" => throw new UnauthorizedException("This is a test unauthorized error"),
            "forbidden" => throw new ForbiddenException("This is a test forbidden error"),
            "conflict" => throw new ConflictException("This is a test conflict error"),
            "external" => throw new ExternalServiceException("TestService", "Service unavailable"),
            _ => throw new Exception("This is a test generic error")
        };
    }
}

// DTOs
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public UserInfo User { get; set; } = new();
}

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class RegisterResponse
{
    public Guid UserId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class UserInfo
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
```

## Step 4: Test the Implementation

After implementing the above changes, you can test the exception handling by making requests to:

### Test Login Endpoint
```bash
# Valid login
curl -X POST "http://localhost:6003/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email": "test@example.com", "password": "password123"}'

# Invalid credentials
curl -X POST "http://localhost:6003/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email": "invalid@test.com", "password": "password123"}'

# Validation error
curl -X POST "http://localhost:6003/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email": "invalid-email", "password": ""}'
```

### Test Register Endpoint
```bash
# Valid registration
curl -X POST "http://localhost:6003/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{"email": "new@example.com", "password": "password123", "name": "New User"}'

# User already exists
curl -X POST "http://localhost:6003/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{"email": "existing@test.com", "password": "password123", "name": "Existing User"}'

# Validation errors
curl -X POST "http://localhost:6003/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{"email": "invalid-email", "password": "123", "name": ""}'
```

### Test Different Error Types
```bash
# Business error
curl "http://localhost:6003/api/auth/test-errors?errorType=business"

# Validation error
curl "http://localhost:6003/api/auth/test-errors?errorType=validation"

# Not found error
curl "http://localhost:6003/api/auth/test-errors?errorType=notfound"

# Generic error
curl "http://localhost:6003/api/auth/test-errors?errorType=generic"
```

## Expected Response Format

All error responses will follow this consistent format:

```json
{
  "success": false,
  "message": "Error message here",
  "data": null,
  "errors": ["Detailed error messages"],
  "timestamp": "2024-01-15T10:30:00Z"
}
```

Success responses will follow this format:

```json
{
  "success": true,
  "message": "Success message",
  "data": { /* actual data */ },
  "errors": [],
  "timestamp": "2024-01-15T10:30:00Z"
}
```

## Benefits

After implementing this system, you'll get:

1. **Consistent Error Responses** - All services return the same error format
2. **Automatic Logging** - All exceptions are logged with correlation IDs
3. **Domain-Specific Exceptions** - Healthcare-specific exception types
4. **Validation Helpers** - Reusable validation logic
5. **Base Classes** - Common functionality for controllers and services
6. **Easy Testing** - Built-in error testing endpoints
