using BookingCare.Services.Hospital.Services;
using BookingCare.Services.Hospital.Data;
using BookingCare.Services.Hospital.Services.Interfaces;
using BookingCare.Services.Hospital.Services.Implementations;
using BookingCare.Services.Hospital.Repositories.Interfaces;
using BookingCare.Services.Hospital.Repositories.Implementations;
using BookingCare.Services.Hospital.Mappings;
using Microsoft.EntityFrameworkCore;
using BookingCare.Shared.Common.Extensions;
using BookingCare.Shared.Common.Versioning;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel with security best practices
builder.WebHost.ConfigureSecureKestrel(builder.Configuration, builder.Environment, "hospital");

// Add services to the container.
builder.Services.AddCommonControllers();
builder.Services.AddCommonSwagger("Hospital");

// Database configuration
builder.Services.AddDbContext<HospitalDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Server=(local);Database=MABS_Hospital;Trusted_Connection=True;TrustServerCertificate=True;"));

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(HospitalMappingProfile));

// Register repositories
builder.Services.AddScoped<IHospitalRepository, HospitalRepository>();
builder.Services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
builder.Services.AddScoped<IHospitalSubscriptionRepository, HospitalSubscriptionRepository>();

// Register services
builder.Services.AddScoped<IHospitalService, HospitalService>();
builder.Services.AddScoped<IHospitalSubscriptionService, HospitalSubscriptionService>();

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

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseCommonSwaggerUI("Hospital");

app.UseGlobalExceptionHandling();
app.UseStandardAuthPipeline();

// Map controllers for REST API
app.MapControllers();

// Map gRPC services
app.MapGrpcService<HospitalGrpcService>();

// Default endpoint
app.MapGet("/", () => "BookingCare Hospital Service is running. REST API: /swagger, gRPC: port 6014");

// Health check endpoint
app.MapCommonHealthCheck("Hospital");

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
