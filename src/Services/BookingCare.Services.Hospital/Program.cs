using BookingCare.Services.Hospital.Services;
using BookingCare.Services.Hospital.Data;
using BookingCare.Services.Hospital.Services.Interfaces;
using BookingCare.Services.Hospital.Services.Implementations;
using BookingCare.Services.Hospital.Repositories.Interfaces;
using BookingCare.Services.Hospital.Repositories.Implementations;
using BookingCare.Services.Hospital.Mappings;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;

// Enable HTTP/2 without TLS for gRPC (development only)
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(6004, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });
    options.ListenAnyIP(6014, listenOptions =>
    {
        // listenOptions.UseHttps();
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "BookingCare Hospital Service API", Version = "v1" });
});

// Add API versioning
builder.Services.AddApiVersioning();

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
builder.Services.AddLogging();

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
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BookingCare Hospital Service API v1");
        c.RoutePrefix = "swagger";
    });
}

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
app.UseRouting();

app.MapControllers();
app.MapGrpcService<HospitalGrpcService>();

app.MapGet("/", () => new
{
    Service = "BookingCare Hospital Service",
    Status = "Running",
    Version = "1.0.0",
    Timestamp = DateTime.UtcNow,
    Environment = app.Environment.EnvironmentName
});

app.MapGet("/health", () => new
{
    Status = "Healthy",
    Service = "Hospital",
    Timestamp = DateTime.UtcNow,
    Environment = app.Environment.EnvironmentName
});

app.Run();
