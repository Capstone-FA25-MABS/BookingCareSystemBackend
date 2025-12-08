using BookingCare.Services.Discount.Data;
using BookingCare.Services.Discount.Mappings;
using BookingCare.Services.Discount.Middlewares;
using BookingCare.Services.Discount.Repositories;
using BookingCare.Services.Discount.Services;
using BookingCare.Shared.Common.Extensions;
using BookingCare.Shared.Common.Versioning;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel with security best practices
builder.WebHost.ConfigureSecureKestrel(builder.Configuration, builder.Environment, "discount");

// Add services
builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddEndpointsApiExplorer();

// Add API versioning support
builder.Services.AddApiVersioningSupport();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1.0", new() { Title = "BookingCare Discount API", Version = "v1.0" });
});

// Add monitoring (Prometheus, Grafana, Jaeger)
builder.Services.AddBookingCareMonitoring("BookingCare.Services.Discount", "1.0.0");

// Add global exception handling (includes monitoring integration)
builder.Services.AddGlobalExceptionHandling();

// Database configuration
builder.Services.AddDbContext<DiscountDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString);
});

// Repository registration
builder.Services.AddScoped<IDiscountRepository, DiscountRepository>();

// Service registration
builder.Services.AddScoped<IDiscountService, DiscountService>();

// Background services
builder.Services.AddHostedService<DiscountExpirationBackgroundService>();

// AutoMapper configuration
builder.Services.AddAutoMapper(typeof(DiscountMappingProfile));

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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BookingCare Discount Service V1");
        c.RoutePrefix = "swagger";
    });
}

// Add monitoring middleware (must be early in pipeline)
app.UseBookingCareMonitoring();

// Add global exception handling (includes monitoring integration)
app.UseGlobalExceptionHandling();

// Add custom middleware
app.UseMiddleware<DiscountExpirationMiddleware>();

// Configure routing
app.UseRouting();

// Authentication and authorization (if needed)
// app.UseAuthentication();
// app.UseAuthorization();

// Map controllers for REST API
app.MapControllers();

// Map gRPC services
app.MapGrpcService<DiscountGrpcService>();

// Default endpoint
app.MapGet(
    "/",
    () => "BookingCare Discount Service is running. REST API: /swagger, gRPC: port 6017"
);

// Health check endpoint
app.MapGet(
    "/health",
    () =>
        Results.Ok(
            new
            {
                Service = "Discount",
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Version = "1.0.0",
            }
        )
);

// Database migration and seeding (development only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<DiscountDbContext>();
        await context.Database.EnsureCreatedAsync();
        app.Logger.LogInformation("Database ensured created successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred while ensuring database creation");
    }
}

app.Run();
