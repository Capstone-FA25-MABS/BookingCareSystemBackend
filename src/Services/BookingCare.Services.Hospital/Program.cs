using BookingCare.Services.Hospital.Services;
using BookingCare.Services.Hospital.Data;
using BookingCare.Services.Hospital.Services.Interfaces;
using BookingCare.Services.Hospital.Services.Implementations;
using BookingCare.Services.Hospital.Repositories.Interfaces;
using BookingCare.Services.Hospital.Repositories.Implementations;
using BookingCare.Services.Hospital.Mappings;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using BookingCare.Shared.Common.Extensions;
using BookingCare.Shared.Common.Versioning;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel with security best practices
builder.WebHost.ConfigureSecureKestrel(builder.Configuration, builder.Environment, "hospital");

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddGrpc();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Register common group names to avoid mismatch
    c.SwaggerDoc("v1", new() { Title = "BookingCare Hospital Service", Version = "v1" });
    c.SwaggerDoc("v1.0", new() { Title = "BookingCare Hospital Service", Version = "v1.0" });

    // Ensure endpoints are included in the correct Swagger doc
    c.DocInclusionPredicate((docName, apiDesc) =>
        string.Equals(docName, apiDesc.GroupName, StringComparison.OrdinalIgnoreCase));
});

// Add global exception handling
builder.Services.AddGlobalExceptionHandling();

// Add API versioning support
builder.Services.AddApiVersioningSupport();

// Add Entity Framework
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=(local);Database=MABS_Hospital;Trusted_Connection=True;TrustServerCertificate=True;";
builder.Services.AddDbContext<HospitalDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(HospitalMappingProfile));

// Register repositories
builder.Services.AddScoped<IHospitalRepository, HospitalRepository>();
builder.Services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
builder.Services.AddScoped<IHospitalSubscriptionRepository, HospitalSubscriptionRepository>();

// Register services
builder.Services.AddScoped<IHospitalService, HospitalService>();
builder.Services.AddScoped<IHospitalSubscriptionService, HospitalSubscriptionService>();

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
    app.UseSwaggerUI(c =>
    {
        provider.ApiVersionDescriptions.ToList().ForEach(description =>
            c.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
                $"BookingCare Hospital API {description.GroupName.ToUpperInvariant()}"));
        c.RoutePrefix = "swagger";
    });
}

// Add global exception handling
app.UseGlobalExceptionHandling();

// Auto-migrate database in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
    try
    {
        context.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while creating the database");
    }
}

app.UseCors("AllowAll");

// Configure routing
app.UseRouting();

// Map controllers for REST API
app.MapControllers();

// Map gRPC services
app.MapGrpcService<HospitalGrpcService>();

// Default endpoint
app.MapGet("/", () => "BookingCare Hospital Service is running. REST API: /swagger, gRPC: port 6014");

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    Service = "Hospital",
    Status = "Healthy",
    Timestamp = DateTime.UtcNow,
    Version = "1.0.0"
}));

app.Run();
