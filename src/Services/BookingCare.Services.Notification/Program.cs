using BookingCare.Services.Notification.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using BookingCare.Shared.EventBus.Extensions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Services.Notification.Handlers;
using BookingCare.Services.Notification.Utils.Email;
using BookingCare.Services.Notification.Utils.SMS;
using BookingCare.Services.Notification.Utils.OTP;
using BookingCare.Services.Notification.Setting;
using BookingCare.Services.Auth.Protos;
using System.Text.Json.Serialization;
using BookingCare.Shared.Common.Extensions;
using BookingCare.Services.Notification.Repositories.Implementations;
using BookingCare.Services.Notification.Repositories.Interfaces;
using BookingCare.Services.Notification.Services.Interfaces;
using BookingCare.Services.Notification.Services.Grpc;
using BookingCare.Shared.Cache.Extensions;
using BookingCare.Shared.Common.Versioning;


// Enable HTTP/2 without TLS for gRPC (development only)
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(6010, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });
    options.ListenAnyIP(6020, listenOptions =>
    {
        // listenOptions.UseHttps();
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddGrpc();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1.0", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "BookingCare Notification API",
        Version = "v1.0",
        Description = "API for notification services including OTP, email, SMS, and push notifications"
    });
});

// Email settings and service
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddSingleton<EmailService>();

// Add Redis cache using shared cache service
builder.Services.AddRedisCache(builder.Configuration);

builder.Services.AddScoped<ManageOtp>();
builder.Services.AddSingleton<EmailTemplate>();

// MongoDB settings
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection(MongoDbSettings.SectionName));

// FCM settings and services
builder.Services.Configure<FcmOptions>(builder.Configuration.GetSection(FcmOptions.SectionName));
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<DeviceStore>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<FcmV1Service>();

// Add JWT Authentication and Authorization using centralized configuration
builder.Services.AddJwtAuthAndAuthorization();

// Add global exception handling
builder.Services.AddGlobalExceptionHandling();

// Add API versioning support
builder.Services.AddApiVersioningSupport();

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// EventBus
builder.Services.AddRabbitMQEventBus(builder.Configuration, "notification-service-queue");
builder.Services.AddIntegrationEventHandler<NotificationSendEventHandler>();

// gRPC client for Auth service
builder.Services.AddGrpcClient<AuthService.AuthServiceClient>(o =>
{
    o.Address = new Uri("http://localhost:6013"); // Auth service gRPC endpoint
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    var provider = app.Services.GetRequiredService<Microsoft.AspNetCore.Mvc.ApiExplorer.IApiVersionDescriptionProvider>();
    app.UseSwaggerUI(c =>
    {
        provider.ApiVersionDescriptions.ToList().ForEach(description =>
            c.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", $"BookingCare Notification API {description.GroupName.ToUpperInvariant()}"));
        c.RoutePrefix = "swagger";
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    });
}
app.UseGlobalExceptionHandling(); // Assuming this is already added via AddGlobalExceptionHandling
app.UseStandardAuthPipeline();

app.MapControllers();

// Configure the HTTP request pipeline.
app.MapGrpcService<OtpGrpcService>();
app.MapGet("/", () => "BookingCare Notification Service is running...");

// Subscribe to email notifications
app.UseEventBus(eventBus =>
{
    eventBus.Subscribe<NotificationSendEvent, NotificationSendEventHandler>();
});

app.Run();