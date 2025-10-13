using BookingCare.Services.Doctor.Data;
using BookingCare.Services.Doctor.Repositories.Interfaces;
using BookingCare.Services.Doctor.Repositories.Implementations;
using BookingCare.Services.Doctor.Services.Interfaces;
using BookingCare.Services.Doctor.Services.Implementations;
using BookingCare.Services.Doctor.Mappings;
using BookingCare.Services.Doctor.Middlewares;
using Microsoft.EntityFrameworkCore;
using BookingCare.Shared.Common.Extensions;
using BookingCare.Services.Favorite;
using BookingCare.Services.Auth.Protos;
using BookingCare.Services.Review.Grpc;
using BookingCare.Services.Doctor.Services.Grpc;
using BookingCare.Shared.Common.Versioning;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel with security best practices
builder.WebHost.ConfigureSecureKestrel(builder.Configuration, builder.Environment, "doctor");

// Add services to the container.
builder.Services.AddCommonControllers();
builder.Services.AddCommonSwagger("Doctor");

// Database configuration
builder.Services.AddDbContext<DoctorDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString);
});

// Repository registration
builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();
builder.Services.AddScoped<IPositionRepository, PositionRepository>();
builder.Services.AddScoped<ISpecialtyRepository, SpecialtyRepository>();
builder.Services.AddScoped<ILanguageRepository, LanguageRepository>();
builder.Services.AddScoped<IServiceTypeRepository, ServiceTypeRepository>();

// Service registration
builder.Services.AddScoped<IDoctorService, DoctorService>();
builder.Services.AddScoped<IPositionService, PositionService>();
builder.Services.AddScoped<ISpecialtyService, SpecialtyService>();
builder.Services.AddScoped<ILanguageService, LanguageService>();
builder.Services.AddScoped<IServiceTypeService, ServiceTypeService>();
builder.Services.AddScoped<ILocationApiService, LocationApiService>();

// AutoMapper configuration
builder.Services.AddAutoMapper(typeof(DoctorMappingProfile), typeof(PositionMappingProfile), typeof(SpecialtyMappingProfile), typeof(SimpleMappingProfile));

// gRPC clients
var favoritesAddress = builder.Configuration.GetSection("GrpcClients:Favorites:Address").Value ?? "http://localhost:6109";
builder.Services.AddGrpcClient<FavoritesService.FavoritesServiceClient>(options =>
{
    options.Address = new Uri(favoritesAddress);
});

var authAddress = builder.Configuration.GetSection("GrpcClients:Auth:Address").Value ?? "http://localhost:6103";
builder.Services.AddGrpcClient<AuthService.AuthServiceClient>(options =>
{
    options.Address = new Uri(authAddress);
});

var hospitalAddress = builder.Configuration.GetSection("GrpcClients:Hospital:Address").Value ?? "http://localhost:6104";
builder.Services.AddGrpcClient<BookingCare.Services.Hospital.HospitalService.HospitalServiceClient>(options =>
{
    options.Address = new Uri(hospitalAddress);
});

var reviewAddress = builder.Configuration.GetSection("GrpcClients:Review:Address").Value ?? "http://localhost:6112";
builder.Services.AddGrpcClient<ReviewService.ReviewServiceClient>(options =>
{
    options.Address = new Uri(reviewAddress);
});

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
app.UseCommonSwaggerUI("Doctor");

app.UseGlobalExceptionHandling();
app.UseStandardAuthPipeline();
// Add custom middleware in order
app.UseMiddleware<DoctorSecurityMiddleware>();
app.UseMiddleware<DoctorRateLimitingMiddleware>();

// Map controllers for REST API
app.MapControllers();

// Map gRPC services
app.MapGrpcService<DoctorGrpcService>();


// Default endpoint
app.MapGet("/", () => "BookingCare Doctor Service is running. REST API: /swagger, gRPC: port 6018");

// Health check endpoint
app.MapCommonHealthCheck("Doctor");

// Database migration and seeding (development only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<DoctorDbContext>();
        await context.Database.EnsureCreatedAsync();
        app.Logger.LogInformation("Database ensured created successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred while ensuring database creation");
    }
}

app.Run();
