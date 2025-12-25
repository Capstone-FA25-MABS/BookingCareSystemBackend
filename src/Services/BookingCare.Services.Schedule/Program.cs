using BookingCare.Services.Schedule.Data;
using BookingCare.Services.Schedule.Repositories;
using BookingCare.Services.Schedule.Services;
using BookingCare.Services.Schedule.Mappings;
using BookingCare.Shared.Cache.Extensions;
using BookingCare.Shared.Common.Extensions;
using BookingCare.Shared.Common.Interfaces;
using BookingCare.Shared.Common.Versioning;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel with security best practices
builder.WebHost.ConfigureSecureKestrel(builder.Configuration, builder.Environment, "schedule");

// Add DbContext
builder.Services.AddDbContext<ScheduleDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Redis Cache
builder.Services.AddRedisCache(builder.Configuration);

// Add gRPC
builder.Services.AddGrpc();

// Add Controllers and API versioning
builder.Services.AddCommonControllers();
builder.Services.AddControllers();
builder.Services.AddApiVersioningSupport();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add global exception handling
builder.Services.AddGlobalExceptionHandling();

// Add JWT Authentication and Authorization using centralized configuration
// This includes: JWT auth, authorization, and frontend configuration
builder.Services.AddJwtAuthAndAuthorization();

// AutoMapper configuration
builder.Services.AddAutoMapper(typeof(ScheduleMappingProfile));

// Register repositories and services
builder.Services.AddScoped<IScheduleRepository, ScheduleRepository>();
builder.Services.AddScoped<IScheduleService, ScheduleService>();
builder.Services.AddScoped<IHoldSlotService, HoldSlotService>();
builder.Services.AddSingleton<BookingCare.Services.Schedule.Services.DatabaseInitializationService>();

// Configure gRPC clients for inter-service communication following ASP.NET Core DI best practices
// Register GrpcClients wrapper to reduce constructor parameter count
builder.Services.AddScoped<GrpcClients>();
var doctorAddress = builder.Configuration.GetSection("GrpcClients:Doctor:Address").Value ?? "http://localhost:6018";
builder.Services.AddGrpcClient<BookingCare.Services.Doctor.Protos.DoctorService.DoctorServiceClient>(options =>
{
    options.Address = new Uri(doctorAddress);
});

var serviceMedicalAddress = builder.Configuration.GetSection("GrpcClients:ServiceMedical:Address").Value ?? "http://localhost:6023";
builder.Services.AddGrpcClient<BookingCare.Services.ServiceMedical.Protos.ServiceMedicalService.ServiceMedicalServiceClient>(options =>
{
    options.Address = new Uri(serviceMedicalAddress);
});

var appointmentAddress = builder.Configuration.GetSection("GrpcClients:Appointment:Address").Value ?? "http://localhost:6102";
builder.Services.AddGrpcClient<BookingCare.Services.Appointment.Protos.AppointmentService.AppointmentServiceClient>(options =>
{
    options.Address = new Uri(appointmentAddress);
});


// Add BookingCare metrics
builder.Services.AddBookingCareMetrics("schedule-service");
var app = builder.Build();

// Enable metrics if configured
var enableMetrics = Environment.GetEnvironmentVariable("ENABLE_PROMETHEUS_METRICS") == "true";
app.UseBookingCareMetrics(enableMetrics);

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Add global exception handling
app.UseGlobalExceptionHandling();
app.UseStandardAuthPipeline();
// Initialize database from SQL script if not exists
using (var scope = app.Services.CreateScope())
{
    var dbInitService = scope.ServiceProvider.GetRequiredService<BookingCare.Services.Schedule.Services.DatabaseInitializationService>();
    await dbInitService.InitializeAsync();
}

app.MapControllers();

// Configure gRPC services
// gRPC service now properly handles GUID conversions
app.MapGrpcService<ScheduleGrpcService>();
app.MapGet("/", () => "BookingCare Schedule Service is running...");

app.Run();
