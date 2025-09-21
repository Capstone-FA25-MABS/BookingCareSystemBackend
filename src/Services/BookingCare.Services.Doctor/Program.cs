using BookingCare.Services.Doctor.Data;
using BookingCare.Services.Doctor.Repositories.Interfaces;
using BookingCare.Services.Doctor.Repositories.Implementations;
using BookingCare.Services.Doctor.Services.Interfaces;
using BookingCare.Services.Doctor.Services.Implementations;
using BookingCare.Services.Doctor.Services;
using BookingCare.Services.Doctor.Mappings;
using BookingCare.Services.Doctor.Middlewares;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using BookingCare.Shared.Common.Extensions;
using BookingCare.Services.Favorite;
using BookingCare.Services.Auth.Protos;
using BookingCare.Services.Doctor.Services.Grpc;
using BookingCare.Shared.Common.Versioning;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

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
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

// Add services
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Register common group names to avoid mismatch
    c.SwaggerDoc("v1", new() { Title = "BookingCare Doctor Service", Version = "v1" });
    c.SwaggerDoc("v1.0", new() { Title = "BookingCare Doctor Service", Version = "v1.0" });

    // Ensure endpoints are included in the correct Swagger doc
    c.DocInclusionPredicate((docName, apiDesc) =>
        string.Equals(docName, apiDesc.GroupName, StringComparison.OrdinalIgnoreCase));
});

// Add global exception handling
builder.Services.AddGlobalExceptionHandling();

// Add API versioning support
builder.Services.AddApiVersioningSupport();

// Add gRPC server
builder.Services.AddGrpc();

// Database configuration
builder.Services.AddDbContext<DoctorDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString);
});

// Repository registration
builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();
builder.Services.AddScoped<IPositionRepository, PositionRepository>();
builder.Services.AddScoped<ILanguageRepository, LanguageRepository>();
builder.Services.AddScoped<IServiceTypeRepository, ServiceTypeRepository>();

// Service registration
builder.Services.AddScoped<IDoctorService, DoctorService>();
builder.Services.AddScoped<IPositionService, PositionService>();
builder.Services.AddScoped<ILanguageService, LanguageService>();
builder.Services.AddScoped<IServiceTypeService, ServiceTypeService>();


// AutoMapper configuration
builder.Services.AddAutoMapper(typeof(DoctorMappingProfile), typeof(PositionMappingProfile));

// gRPC clients
var favoritesAddress = builder.Configuration.GetSection("GrpcClients:Favorites:Address").Value ?? "http://localhost:6019";
builder.Services.AddGrpcClient<FavoritesService.FavoritesServiceClient>(options =>
{
    options.Address = new Uri(favoritesAddress);
});

var authAddress = builder.Configuration.GetSection("GrpcClients:Auth:Address").Value ?? "http://localhost:6001";
builder.Services.AddGrpcClient<AuthService.AuthServiceClient>(options =>
{
    options.Address = new Uri(authAddress);
});

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

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
                $"BookingCare Doctor API {description.GroupName.ToUpperInvariant()}"));
        c.RoutePrefix = "swagger";
    });
}

// Add global exception handling
app.UseGlobalExceptionHandling();

// Add custom middleware in order
app.UseMiddleware<DoctorSecurityMiddleware>();
app.UseMiddleware<DoctorRateLimitingMiddleware>();

// Configure routing
app.UseRouting();


// Map controllers for REST API
app.MapControllers();

// Map gRPC services
app.MapGrpcService<DoctorGrpcService>();


// Default endpoint
app.MapGet("/", () => "BookingCare Doctor Service is running. REST API: /swagger, gRPC: port 6018");

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    Service = "Doctor",
    Status = "Healthy",
    Timestamp = DateTime.UtcNow,
    Version = "1.0.0"
}));

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
