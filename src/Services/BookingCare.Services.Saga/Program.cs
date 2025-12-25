using BookingCare.Shared.Common.Extensions;
using BookingCare.Shared.Common.Interfaces;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Shared.Saga.Extensions;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel with security best practices
builder.WebHost.ConfigureSecureKestrel(builder.Configuration, builder.Environment, "saga");

// Add JWT Authentication and Authorization using centralized configuration
builder.Services.AddJwtAuthAndAuthorization();
// Add global exception handling
builder.Services.AddGlobalExceptionHandling();

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add API versioning support
builder.Services.AddApiVersioningSupport();

// Add Saga orchestration services
builder.Services.AddSagaOrchestration(builder.Configuration);

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1.0", new() { Title = "BookingCare Saga API", Version = "v1.0" });
});


// Add BookingCare metrics
builder.Services.AddBookingCareMetrics("saga-service");
var app = builder.Build();

// Enable metrics if configured
var enableMetrics = Environment.GetEnvironmentVariable("ENABLE_PROMETHEUS_METRICS") == "true";
app.UseBookingCareMetrics(enableMetrics);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseGlobalExceptionHandling();
app.UseStandardAuthPipeline();
app.MapControllers();

// Configure gRPC services
app.MapGet("/", () => "BookingCare Saga Service is running...");

app.Run();
