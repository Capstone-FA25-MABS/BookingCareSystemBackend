using BookingCare.Services.Doctor.Data;
using BookingCare.Services.Doctor.Repositories;
using BookingCare.Services.Doctor.Services;
using BookingCare.Services.Doctor.Mappings;
using BookingCare.Services.Doctor.Middlewares;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using BookingCare.Shared.Common.Extensions;

// Enable HTTP/2 without TLS for gRPC (development only)
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel for dual HTTP/gRPC support
builder.WebHost.ConfigureKestrel(options =>
{
    // HTTP endpoint for REST API
    options.ListenAnyIP(6008, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });
    
    // gRPC endpoint
    options.ListenAnyIP(6018, listenOptions =>
    {
        // listenOptions.UseHttps(); // Enable in production
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

// Add services
builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "BookingCare Doctor Service", Version = "v1" });
});

// Add global exception handling
builder.Services.AddGlobalExceptionHandling();

// Database configuration
builder.Services.AddDbContext<DoctorDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString);
});

// Repository registration
builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();

// Service registration
builder.Services.AddScoped<IDoctorService, DoctorService>();

// Background services
builder.Services.AddHostedService<DoctorBackgroundService>();

// AutoMapper configuration
builder.Services.AddAutoMapper(typeof(DoctorMappingProfile));

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BookingCare Doctor Service V1");
        c.RoutePrefix = "swagger";
    });
}

// Add global exception handling
app.UseGlobalExceptionHandling();

// Add custom middleware in order
app.UseMiddleware<DoctorSecurityMiddleware>();
app.UseMiddleware<DoctorRateLimitingMiddleware>();
app.UseMiddleware<DoctorValidationMiddleware>();

// Configure routing
app.UseRouting();

// Authentication and authorization (if needed)
// app.UseAuthentication();
// app.UseAuthorization();

// Map controllers for REST API
app.MapControllers();

// Map gRPC services
app.MapGrpcService<DoctorGrpcService>();

// Default endpoint
app.MapGet("/", () => "BookingCare Doctor Service is running!");

// Health check endpoint
app.MapGet("/health", () => new { Status = "Healthy", Service = "Doctor", Timestamp = DateTime.UtcNow });

app.Run();
