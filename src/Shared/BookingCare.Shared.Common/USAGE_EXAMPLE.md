# Example: Configuring Auth Service with Global Exception Handling

Here's how to configure the Auth service to use the global exception handling system:

## 1. Update Auth Service Project Reference

First, add a reference to the shared common library in your Auth service:

```xml
<!-- In MicroGrpcDemo.Services.Auth.csproj -->
<ItemGroup>
  <ProjectReference Include="..\..\Shared\BookingCare.Shared.Common\BookingCare.Shared.Common.csproj" />
</ItemGroup>
```

## 2. Update Program.cs

```csharp
using BookingCare.Shared.Common.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddGlobalExceptionHandling(); // Add this line

// Your existing services
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add other services...

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseGlobalExceptionHandling(); // Add this early in pipeline

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
```

## 3. Update Controllers

```csharp
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Exceptions.Domain;

[Route("api/[controller]")]
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return Success(result, "Login successful");
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        return Created(result, "User registered successfully");
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken(RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request);
        return Success(result, "Token refreshed successfully");
    }
}
```

## 4. Update Services

```csharp
using BookingCare.Shared.Common.Services;
using BookingCare.Shared.Common.Exceptions.Domain;
using BookingCare.Shared.Common.Helpers;

public class AuthService : BaseService, IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        ILogger<AuthService> logger) : base(logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            // Validate input
            ValidateRequiredString(request.Email, nameof(request.Email));
            ValidateRequiredString(request.Password, nameof(request.Password));

            if (!ValidationHelper.IsValidEmail(request.Email))
            {
                throw new ValidationException(ValidationHelper.InvalidEmail("Email", request.Email));
            }

            // Find user
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                throw new AuthExceptions.InvalidCredentialsException();
            }

            // Verify password
            if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                throw new AuthExceptions.InvalidCredentialsException();
            }

            // Check account status
            if (user.IsLocked)
            {
                throw new AuthExceptions.AccountLockedException();
            }

            if (!user.EmailVerified)
            {
                throw new AuthExceptions.EmailNotVerifiedException();
            }

            // Generate tokens
            var accessToken = _jwtTokenService.GenerateAccessToken(user);
            var refreshToken = _jwtTokenService.GenerateRefreshToken(user);

            return new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = 3600,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    Role = user.Role
                }
            };

        }, "LoginUser", GetCorrelationId());
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            // Validate input
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

            // Check if user already exists
            var existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
            {
                throw new UserExceptions.UserAlreadyExistsException(request.Email);
            }

            // Create user
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                Name = request.Name,
                PasswordHash = _passwordHasher.HashPassword(request.Password),
                Role = "User",
                CreatedAt = DateTime.UtcNow,
                EmailVerified = false
            };

            await _userRepository.CreateAsync(user);

            // Send verification email (you would implement this)
            // await _emailService.SendVerificationEmailAsync(user.Email, verificationToken);

            return new RegisterResponse
            {
                UserId = user.Id,
                Message = "Registration successful. Please check your email for verification."
            };

        }, "RegisterUser", GetCorrelationId());
    }

    public async Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            ValidateRequiredString(request.RefreshToken, nameof(request.RefreshToken));

            // Validate refresh token
            var principal = _jwtTokenService.ValidateRefreshToken(request.RefreshToken);
            if (principal == null)
            {
                throw new AuthExceptions.InvalidTokenException("Invalid refresh token");
            }

            var userId = principal.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                throw new AuthExceptions.InvalidTokenException("Invalid token claims");
            }

            var user = await _userRepository.GetByIdAsync(userGuid);
            if (user == null || user.IsLocked)
            {
                throw new AuthExceptions.InvalidTokenException("User not found or locked");
            }

            // Generate new tokens
            var accessToken = _jwtTokenService.GenerateAccessToken(user);
            var refreshToken = _jwtTokenService.GenerateRefreshToken(user);

            return new RefreshTokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = 3600
            };

        }, "RefreshToken", GetCorrelationId());
    }
}
```

## 5. Example Error Responses

When exceptions are thrown, they will automatically be converted to consistent JSON responses:

### Invalid Credentials (401):
```json
{
  "success": false,
  "message": "Invalid credentials provided",
  "data": null,
  "errors": ["Invalid credentials provided"],
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### Validation Error (400):
```json
{
  "success": false,
  "message": "Registration validation failed",
  "data": null,
  "errors": [
    "Email: Invalid email format",
    "Password: Password must be at least 8 characters long"
  ],
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### User Already Exists (409):
```json
{
  "success": false,
  "message": "User with email 'user@example.com' already exists",
  "data": null,
  "errors": ["User with email 'user@example.com' already exists"],
  "timestamp": "2024-01-15T10:30:00Z"
}
```

## 6. Logging Output

All exceptions will be automatically logged with context:

```
[Warning] Exception occurred - User: anonymous, Method: POST, Path: /api/auth/login, CorrelationId: 12345, Exception: InvalidCredentialsException

[Error] Exception occurred - User: john.doe@example.com, Method: POST, Path: /api/auth/register, CorrelationId: 67890, Exception: ValidationException
```

This configuration provides comprehensive exception handling with:
- Consistent error responses
- Automatic logging with correlation IDs
- Domain-specific exception types
- Validation helpers
- Base controller and service classes for common functionality
