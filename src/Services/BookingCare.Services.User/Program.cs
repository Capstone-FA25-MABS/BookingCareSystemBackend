using BookingCare.Services.User.Configuration;
using BookingCare.Services.User.Data;
using BookingCare.Services.User.Handlers;
using BookingCare.Services.User.Mappings;
using BookingCare.Services.User.Repositories;
using BookingCare.Services.User.Services;
using BookingCare.Shared.Common.Extensions;
using BookingCare.Shared.Common.Interfaces;
using BookingCare.Shared.Common.Interfaces;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Shared.EventBus.Extensions;
using BookingCare.Shared.FileUpload.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel with security best practices
builder.WebHost.ConfigureSecureKestrel(builder.Configuration, builder.Environment, "user");

// Add DbContext
builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register UserService Configuration
builder.Services.Configure<UserServiceConfiguration>(
    builder.Configuration.GetSection(UserServiceConfiguration.SectionName));

// Register AutoMapper
builder.Services.AddAutoMapper(typeof(UserMappingProfile));

// Register repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPatientRelativeRepository, PatientRelativeRepository>();

// Register services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPatientRelativeService, PatientRelativeService>();
builder.Services.AddSingleton<DatabaseInitializationService>();

// Add S3 File Upload Service
builder.Services.AddS3FileUpload(builder.Configuration);

// Add controllers with common configuration
builder.Services.AddControllers();

// Register Event Handlers
builder.Services.AddIntegrationEventHandler<UserEmailPhoneSyncFailedEventHandler>();
builder.Services.AddIntegrationEventHandler<UserEmailPhoneSyncCompletedEventHandler>();

// Add Event Bus (RabbitMQ)
builder.Services.AddRabbitMQEventBus(builder.Configuration, "user-service-queue");

// Add JWT Authentication and Authorization using centralized configuration
builder.Services.AddJwtAuthAndAuthorization();
// Add global exception handling
builder.Services.AddGlobalExceptionHandling();

// Add logging
builder.Logging.AddCommonLogging();

// Add controllers and Swagger
builder.Services.AddCommonSwagger("User");

builder.Services.AddGrpc();

// Add API versioning support
builder.Services.AddApiVersioningSupport();

// Add BookingCare metrics
builder.Services.AddBookingCareMetrics("user-service");

var app = builder.Build();

// Enable metrics if configured
var enableMetrics = Environment.GetEnvironmentVariable("ENABLE_PROMETHEUS_METRICS") == "true";
app.UseBookingCareMetrics(enableMetrics);

// Configure the HTTP request pipeline
app.UseCommonSwaggerUI("User");

// Initialize database from SQL script if not exists
using (var scope = app.Services.CreateScope())
{
    var dbInitService = scope.ServiceProvider.GetRequiredService<DatabaseInitializationService>();
    await dbInitService.InitializeAsync();
}
app.UseGlobalExceptionHandling();
app.UseStandardAuthPipeline();
app.MapControllers();

// Configure gRPC services
app.MapGrpcService<UserGrpcService>();

// Map health check endpoint
app.MapCommonHealthCheck("User");

// Configure EventBus subscriptions
app.UseEventBus(eventBus =>
{
    // Subscribe to Auth Service sync results
    eventBus.Subscribe<UserEmailPhoneSyncFailedEvent, UserEmailPhoneSyncFailedEventHandler>();
    eventBus.Subscribe<UserEmailPhoneSyncCompletedEvent, UserEmailPhoneSyncCompletedEventHandler>();
});

app.Run();
