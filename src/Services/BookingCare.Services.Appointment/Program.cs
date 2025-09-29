using BookingCare.Services.Appointment.Data;
using BookingCare.Services.Appointment.Services;
using BookingCare.Services.Appointment.Repositories;
using BookingCare.Services.Appointment.Mappings;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using BookingCare.Shared.Common.Extensions;
using BookingCare.Shared.Common.Versioning;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using BookingCare.Shared.Common.AppRouting;

// Enable HTTP/2 without TLS for gRPC (development only)
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    // HTTP endpoint for REST API
    options.ListenAnyIP(6002, listenOptions =>
    {
        //listenOptions.UseHttps();
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });

    // gRPC endpoint
    options.ListenAnyIP(6012, listenOptions =>
    {
        // listenOptions.UseHttps();
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add DbContext
builder.Services.AddDbContext<AppointmentDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(AppointmentMappingProfile));

// Add Repository
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();

// Add Services
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<DataInitializationService>();

// Add global exception handling
builder.Services.AddGlobalExceptionHandling();

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add JWT Authentication and Authorization using centralized configuration
// This includes: JWT auth, authorization, and frontend configuration
builder.Services.AddJwtAuthAndAuthorization();

// Add gRPC
builder.Services.AddGrpc();

builder.Services.AddEndpointsApiExplorer();

// Add API versioning support
builder.Services.AddApiVersioningSupport();

builder.Services.AddSwaggerGen(c =>
{
    // Register common group names to avoid mismatch (some setups produce v1 instead of v1.0)
    c.SwaggerDoc("v1", new() { Title = "BookingCare Appointment API", Version = "v1" });
    c.SwaggerDoc("v1.0", new() { Title = "BookingCare Appointment API", Version = "v1.0" });
    // Ensure endpoints are included in the correct Swagger doc based on ApiExplorer group name (e.g., v1.0)
    c.DocInclusionPredicate((docName, apiDesc) =>
        string.Equals(docName, apiDesc.GroupName, StringComparison.OrdinalIgnoreCase));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
    app.UseSwaggerUI(c =>
    {
        provider.ApiVersionDescriptions.ToList().ForEach(description =>
            c.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", $"BookingCare Appointment API {description.GroupName.ToUpperInvariant()}"));
        c.RoutePrefix = "swagger";
    });
}

app.UseGlobalExceptionHandling();
app.UseStandardAuthPipeline();

app.MapControllers();

// Map gRPC services
app.MapGrpcService<GreeterService>();
app.MapGet("/", () => "BookingCare Appointment Service is running...");

// Initialize default data
if (app.Environment.IsDevelopment())
{
    try
    {
        using var scope = app.Services.CreateScope();
        var dataInitializationService = scope.ServiceProvider.GetRequiredService<DataInitializationService>();
        await dataInitializationService.InitializeDefaultDataAsync();
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error initializing default data");
    }
}

app.Run();
