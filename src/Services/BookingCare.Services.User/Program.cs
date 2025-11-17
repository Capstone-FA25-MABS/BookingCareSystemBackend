using BookingCare.Services.User.Configuration;
using BookingCare.Services.User.Data;
using BookingCare.Services.User.Handlers;
using BookingCare.Services.User.Mappings;
using BookingCare.Services.User.Repositories;
using BookingCare.Services.User.Services;
using BookingCare.Shared.Common.Extensions;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Shared.EventBus.Extensions;
using BookingCare.Shared.FileUpload.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel with security best practices

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

// Register services
builder.Services.AddScoped<IUserService, UserService>();

// Add S3 File Upload Service
builder.Services.AddS3FileUpload(builder.Configuration);

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

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseCommonSwaggerUI("User");

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    await context.Database.EnsureCreatedAsync();
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
