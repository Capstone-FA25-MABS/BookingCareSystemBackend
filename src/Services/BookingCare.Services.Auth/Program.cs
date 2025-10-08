using BookingCare.Services.Auth.Data;
using BookingCare.Services.Auth.Handlers;
using BookingCare.Services.Auth.Models.Entities;
using BookingCare.Services.Auth.Repositories;
using BookingCare.Services.Auth.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BookingCare.Services.Auth.Mappings;
using BookingCare.Services.Notification.Protos;
using BookingCare.Services.Auth.Utils;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Shared.EventBus.Extensions;
using BookingCare.Shared.Common.AppRouting;
using BookingCare.Shared.Common.Extensions;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Services.Auth.Providers;
using BookingCare.Shared.Saga.Extensions;
using BookingCare.Shared.Saga.Steps;
using BookingCare.Shared.Saga.SagaDefinition;
using BookingCare.Services.Doctor.Protos;
using BookingCare.Services.User.Protos;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel with security best practices
builder.WebHost.ConfigureSecureKestrel(builder.Configuration, builder.Environment, "auth");

// Add services to the container using common extensions
builder.Services.AddCommonControllers();
builder.Services.AddCommonSwagger("Auth");

// Add DbContext
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity
builder.Services.AddIdentity<AccountEntity, RoleEntity>(options =>
{
    // Cấu hình mật khẩu:
    // - RequireDigit: Yêu cầu phải có ít nhất một chữ số trong mật khẩu.
    options.Password.RequireDigit = true;
    // - RequireLowercase: Yêu cầu phải có ít nhất một ký tự thường.
    options.Password.RequireLowercase = true;
    // - RequireNonAlphanumeric: Yêu cầu phải có ít nhất một ký tự đặc biệt (không phải chữ hoặc số).
    options.Password.RequireNonAlphanumeric = true;
    // - RequireUppercase: Yêu cầu phải có ít nhất một ký tự in hoa.
    options.Password.RequireUppercase = true;
    // - RequiredLength: Độ dài tối thiểu của mật khẩu là 8 ký tự.
    options.Password.RequiredLength = 8;
    // - RequiredUniqueChars: Số lượng ký tự khác nhau tối thiểu trong mật khẩu là 1.
    options.Password.RequiredUniqueChars = 1;

    // Cấu hình khóa tài khoản:
    // - DefaultLockoutTimeSpan: Thời gian khóa tài khoản mặc định là 5 phút khi đăng nhập sai quá số lần cho phép.
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    // - MaxFailedAccessAttempts: Số lần đăng nhập sai tối đa trước khi bị khóa là 5 lần.
    options.Lockout.MaxFailedAccessAttempts = 5;
    // - AllowedForNewUsers: Cho phép áp dụng chính sách khóa tài khoản cho người dùng mới.
    options.Lockout.AllowedForNewUsers = true;

    // Cấu hình người dùng: 
    // - RequireUniqueEmail: Yêu cầu email phải là duy nhất.
    options.User.RequireUniqueEmail = true;

})
.AddEntityFrameworkStores<AuthDbContext>()
.AddDefaultTokenProviders();

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    // Reset password token expires based on configuration
    options.TokenLifespan = TimeSpan.FromHours(1);
});

// Add HttpContextAccessor for cookie management
builder.Services.AddHttpContextAccessor();

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(AuthMappingProfile));

// Add Repository
builder.Services.AddScoped<IAuthRepository, AuthRepository>();

// Add Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<AuthGrpcService>();
builder.Services.AddScoped<DataInitializationService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<CookieService>();
// Register External Auth Providers
builder.Services.AddHttpClient<GoogleAuthProvider>();
builder.Services.AddHttpClient<FacebookAuthProvider>();
builder.Services.AddScoped<GoogleAuthProvider>();
builder.Services.AddScoped<FacebookAuthProvider>();
builder.Services.AddScoped<IExternalAuthProvider, GoogleAuthProvider>();
builder.Services.AddScoped<IExternalAuthProvider, FacebookAuthProvider>();
builder.Services.AddScoped<ExternalAuthProviderService>();

// Add gRPC client for OTP verification
builder.Services.AddGrpcClient<OtpVerifier.OtpVerifierClient>(o =>
{
    var endpoint = builder.Configuration.GetSection("OtpVerification").GetValue<string>("GrpcEndpoint") ?? "http://localhost:6020";
    o.Address = new Uri(endpoint);
});

// Add gRPC client for User service
builder.Services.AddGrpcClient<UserService.UserServiceClient>(o =>
{
    var endpoint = builder.Configuration.GetSection("Services:User").GetValue<string>("GrpcUrl") ?? "http://localhost:6024";
    o.Address = new Uri(endpoint);
});

// Add gRPC client for Doctor service  
builder.Services.AddGrpcClient<DoctorService.DoctorServiceClient>(o =>
{
    var endpoint = builder.Configuration.GetSection("Services:Doctor").GetValue<string>("GrpcUrl") ?? "http://localhost:6018";
    o.Address = new Uri(endpoint);
});

// Add global exception handling
builder.Services.AddGlobalExceptionHandling();

// Add logging
builder.Logging.AddCommonLogging();

// Add JWT Authentication and Authorization using centralized configuration
// This includes: JWT auth, authorization, and frontend configuration
builder.Services.AddJwtAuthAndAuthorization();

// Add gRPC
builder.Services.AddGrpc();

// Add API versioning support
builder.Services.AddApiVersioningSupport();


// Add Saga Orchestration
builder.Services.AddSagaOrchestration(builder.Configuration);

// Register Saga Definitions
builder.Services.AddSaga<UserRegistrationSaga>();
builder.Services.AddSaga<DoctorRegistrationSaga>();
builder.Services.AddSaga<ExternalUserRegistrationSaga>();

// Register Saga Steps
builder.Services.AddSagaStep<CreateAccountGrpcStep>();
builder.Services.AddSagaStep<CreateExternalAccountGrpcStep>();
builder.Services.AddSagaStep<CreateUserProfileGrpcStep>();
builder.Services.AddSagaStep<CreateDoctorProfileGrpcStep>();

// Register Event Handlers
builder.Services.AddIntegrationEventHandler<UserEmailPhoneSyncEventHandler>();

// Add Event Bus (RabbitMQ)
builder.Services.AddRabbitMQEventBus(builder.Configuration, "auth-service-queue");

// Optional Redis (to validate verification flags if needed later)
var redisEnabled = builder.Configuration.GetSection("Redis").GetValue<bool>("Enabled");
if (redisEnabled)
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration.GetSection("Redis").GetValue<string>("Configuration");
        options.InstanceName = builder.Configuration.GetSection("Redis").GetValue<string>("InstanceName");
    });
}

// Bind Frontend options for base URL resolution
builder.Services.Configure<FrontendOptions>(builder.Configuration.GetSection(FrontendOptions.SectionName));

var app = builder.Build();

// Initialize database and default data
try
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();

    // 1. Initialize Saga Database (runs in all environments)
    logger.LogInformation("Initializing Saga database...");
    try
    {
        await app.Services.InitializeSagaDatabaseAsync();
        logger.LogInformation("Saga database initialized successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error initializing Saga database");
        // Don't throw - Auth service can still work without Saga in some scenarios
    }

    // 2. Initialize default data (development only)
    if (app.Environment.IsDevelopment())
    {
        logger.LogInformation("Initializing default data...");
        try
        {
            using var scope = app.Services.CreateScope();
            var dataInitializationService = scope.ServiceProvider.GetRequiredService<DataInitializationService>();
            await dataInitializationService.InitializeDefaultDataAsync();
            logger.LogInformation("Default data initialized successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error initializing default data");
        }
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Critical error during initialization");
}

// Configure the HTTP request pipeline
app.UseCommonSwaggerUI("Auth");

app.UseGlobalExceptionHandling();
app.UseStandardAuthPipeline();

app.MapControllers();

// Map gRPC services
app.MapGrpcService<AuthGrpcService>();

// Map health check endpoint
app.MapCommonHealthCheck("Auth");

// Configure EventBus subscriptions
app.UseEventBus(eventBus =>
{
    // Subscribe to User Service sync requests
    eventBus.Subscribe<UserEmailPhoneSyncRequestedEvent, UserEmailPhoneSyncEventHandler>();
});

app.Run();

