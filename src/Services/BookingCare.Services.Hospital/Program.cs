using BookingCare.Services.Auth.Protos;
using BookingCare.Services.Doctor.Protos;
using BookingCare.Services.Hospital.Data;
using BookingCare.Services.Hospital.Handlers;
using BookingCare.Services.Hospital.Mappings;
using BookingCare.Services.Hospital.Repositories.Implementations;
using BookingCare.Services.Hospital.Repositories.Interfaces;
using BookingCare.Services.Hospital.Services;
using BookingCare.Services.Hospital.Services.Helpers;
using BookingCare.Services.Hospital.Services.Implementations;
using BookingCare.Services.Hospital.Services.Interfaces;
using BookingCare.Services.ServiceMedical.Protos;
using BookingCare.Shared.Common.Extensions;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Shared.EventBus.Extensions;
using BookingCare.Shared.FileUpload.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel with security best practices
builder.WebHost.ConfigureSecureKestrel(builder.Configuration, builder.Environment, "hospital");

// Add services to the container.
builder.Services.AddCommonControllers();
builder.Services.AddCommonSwagger("Hospital");

// Database configuration
builder.Services.AddDbContext<HospitalDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
            ?? "Server=(local);Database=MABS_Hospital;Trusted_Connection=True;TrustServerCertificate=True;"
    )
);

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(HospitalMappingProfile));

// Add Memory Cache
builder.Services.AddMemoryCache();

// Register repositories
builder.Services.AddScoped<IHospitalRepository, HospitalRepository>();
builder.Services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
builder.Services.AddScoped<IHospitalSubscriptionRepository, HospitalSubscriptionRepository>();
builder.Services.AddScoped<IHospitalRegistrationRepository, HospitalRegistrationRepository>();
builder.Services.AddScoped<IHospitalImageRepository, HospitalImageRepository>();

// Register services
builder.Services.AddScoped<BookingCare.Shared.Common.Interfaces.ILocationApiService, BookingCare.Shared.Common.Services.LocationApiService>();
builder.Services.AddScoped<IHospitalService, HospitalService>();
builder.Services.AddScoped<ISubscriptionPlanService, SubscriptionPlanService>();
builder.Services.AddScoped<IHospitalSubscriptionService, HospitalSubscriptionService>();
builder.Services.AddScoped<ISubscriptionUsageService, SubscriptionUsageService>();
builder.Services.AddScoped<ILocationApiService, LocationApiService>();
builder.Services.AddScoped<IHospitalRegistrationService, HospitalRegistrationService>();

// Add Event Bus (RabbitMQ) for message queue
builder.Services.AddRabbitMQEventBus(builder.Configuration, "hospital-service-queue");

// Register Event Handlers
builder.Services.AddIntegrationEventHandler<HospitalRegistrationFilesUploadEventHandler>();
builder.Services.AddIntegrationEventHandler<HospitalAccountCreationFailedEventHandler>();
builder.Services.AddIntegrationEventHandler<HospitalRegistrationAccountLinkedEventHandler>();

// Add S3 File Upload services
builder.Services.AddS3FileUpload(builder.Configuration);

// Add global exception handling
builder.Services.AddGlobalExceptionHandling();

// Add JWT Authentication and Authorization using centralized configuration
// This includes: JWT auth, authorization, and frontend configuration
builder.Services.AddJwtAuthAndAuthorization();

// Add logging
builder.Logging.AddCommonLogging();

// Add API versioning support
builder.Services.AddApiVersioningSupport();

// Add gRPC
builder.Services.AddGrpc();

// gRPC clients
var authAddress =
    builder.Configuration.GetSection("GrpcClients:Auth:Address").Value ?? "http://localhost:6103";
builder.Services.AddGrpcClient<AuthService.AuthServiceClient>(options =>
{
    options.Address = new Uri(authAddress);
});

var doctorAddress =
    builder.Configuration.GetSection("GrpcClients:Doctor:Address").Value ?? "http://localhost:6108";
builder.Services.AddGrpcClient<DoctorService.DoctorServiceClient>(options =>
{
    options.Address = new Uri(doctorAddress);
});

var serviceMedicalAddress =
    builder.Configuration.GetSection("GrpcClients:ServiceMedical:Address").Value
    ?? "http://localhost:6115";
builder.Services.AddGrpcClient<ServiceMedicalService.ServiceMedicalServiceClient>(options =>
{
    options.Address = new Uri(serviceMedicalAddress);
});

// Register HospitalServiceDependencies to reduce constructor parameters
builder.Services.AddScoped<HospitalServiceDependencies>(sp =>
{
    var authClient = sp.GetRequiredService<AuthService.AuthServiceClient>();
    var doctorClient = sp.GetRequiredService<DoctorService.DoctorServiceClient>();
    var serviceMedicalClient =
        sp.GetRequiredService<ServiceMedicalService.ServiceMedicalServiceClient>();
    var locationApiService = sp.GetRequiredService<ILocationApiService>();
    return new HospitalServiceDependencies(
        authClient,
        doctorClient,
        serviceMedicalClient,
        locationApiService
    );
});

// Register SubscriptionServices to reduce constructor parameters
builder.Services.AddScoped<SubscriptionServices>(sp =>
{
    var subscriptionPlanRepository = sp.GetRequiredService<ISubscriptionPlanRepository>();
    var hospitalSubscriptionService = sp.GetRequiredService<IHospitalSubscriptionService>();
    var subscriptionUsageService = sp.GetRequiredService<ISubscriptionUsageService>();
    return new SubscriptionServices(
        subscriptionPlanRepository,
        hospitalSubscriptionService,
        subscriptionUsageService
    );
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseCommonSwaggerUI("Hospital");

app.UseGlobalExceptionHandling();
app.UseStandardAuthPipeline();

// Map controllers for REST API
app.MapControllers();

// Map gRPC services
app.MapGrpcService<HospitalGrpcService>();
app.MapGrpcService<HospitalSubscriptionGrpcService>();
app.MapGrpcService<SubscriptionUsageGrpcService>();

// Default endpoint
app.MapGet(
    "/",
    () => "BookingCare Hospital Service is running. REST API: /swagger, gRPC: port 6014"
);

// Health check endpoint
app.MapCommonHealthCheck("Hospital");

// Configure EventBus subscriptions
app.UseEventBus(eventBus =>
{
    eventBus.Subscribe<
        HospitalRegistrationFilesUploadEvent,
        HospitalRegistrationFilesUploadEventHandler
    >();
    eventBus.Subscribe<
        HospitalAccountCreationFailedEvent,
        HospitalAccountCreationFailedEventHandler
    >();
    eventBus.Subscribe<
        HospitalRegistrationAccountLinkedEvent,
        HospitalRegistrationAccountLinkedEventHandler
    >();
});

// Database migration and seeding (development only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
        await context.Database.EnsureCreatedAsync();
        app.Logger.LogInformation("Database ensured created successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred while ensuring database creation");
    }
}

await app.RunAsync();
