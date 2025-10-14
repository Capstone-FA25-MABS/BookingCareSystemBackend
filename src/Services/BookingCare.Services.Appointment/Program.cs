using BookingCare.Services.Appointment.Data;
using BookingCare.Services.Appointment.Services;
using BookingCare.Services.Appointment.Services.Grpc;
using BookingCare.Services.Appointment.Repositories;
using BookingCare.Services.Appointment.Mappings;
using BookingCare.Services.Appointment.Handlers;
using Microsoft.EntityFrameworkCore;
using BookingCare.Shared.Common.Extensions;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Shared.EventBus.Extensions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Shared.FileUpload.Extensions;


var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel with security best practices
builder.WebHost.ConfigureSecureKestrel(builder.Configuration, builder.Environment, "appointment");

// Add services to the container using common extensions
builder.Services.AddCommonControllers();
builder.Services.AddCommonSwagger("Appointment");

// Add DbContext
builder.Services.AddDbContext<AppointmentDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpContextAccessor();

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(AppointmentMappingProfile));

// Add Repository
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();

// Add Services
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<DataInitializationService>();

// Add S3 File Upload Service
builder.Services.AddS3FileUpload(builder.Configuration);

// Add gRPC client for Doctor service  
builder.Services.AddGrpcClient<BookingCare.Services.Doctor.Protos.DoctorService.DoctorServiceClient>(o =>
{
    var endpoint = builder.Configuration.GetSection("Services:Doctor").GetValue<string>("GrpcUrl") ?? "http://localhost:6108";
    o.Address = new Uri(endpoint);
});

// Add gRPC client for Hospital service  
builder.Services.AddGrpcClient<BookingCare.Services.Hospital.HospitalService.HospitalServiceClient>(o =>
{
    var endpoint = builder.Configuration.GetSection("Services:Hospital").GetValue<string>("GrpcUrl") ?? "http://localhost:6104";
    o.Address = new Uri(endpoint);
});

// Add gRPC client for User service  
builder.Services.AddGrpcClient<BookingCare.Services.User.Protos.UserService.UserServiceClient>(o =>
{
    var endpoint = builder.Configuration.GetSection("Services:User").GetValue<string>("GrpcUrl") ?? "http://localhost:6116";
    o.Address = new Uri(endpoint);
});

// Add EventBus for publishing appointment events and consuming payment events
builder.Services.AddRabbitMQEventBus(builder.Configuration, "appointment-service-queue");

// Register Event Handlers
builder.Services.AddIntegrationEventHandler<AppointmentDeleteRequestedEventHandler>();

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

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseCommonSwaggerUI("Appointment");

app.UseGlobalExceptionHandling();
app.UseStandardAuthPipeline();

app.MapControllers();

// Map gRPC service
app.MapGrpcService<AppointmentGrpcService>();

// Map health check endpoint
app.MapCommonHealthCheck("Appointment");

// Configure EventBus subscriptions
app.UseEventBus(eventBus =>
{
    // Subscribe to appointment deletion requests when payment fails
    eventBus.Subscribe<AppointmentDeleteRequestedIntegrationEvent, AppointmentDeleteRequestedEventHandler>();
});

// Initialize default data
if (app.Environment.IsDevelopment())
{
    try
    {
        using var scope = app.Services.CreateScope();
        var dataInitializationService = scope.ServiceProvider.GetRequiredService<DataInitializationService>();
        await dataInitializationService.InitializeDefaultDataAsync();
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error initializing default data");
    }
}

app.Run();
