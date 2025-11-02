using BookingCare.Services.Auth.Protos; // Add Auth gRPC proto
using BookingCare.Services.Communication.Configuration;
using BookingCare.Services.Communication.Extensions;
using BookingCare.Services.Communication.Handlers; // 🎯 Add for event handlers
using BookingCare.Services.Communication.Hubs;
using BookingCare.Services.Communication.Services;
using BookingCare.Services.Communication.Services.Implementations;
using BookingCare.Services.Communication.Services.Interfaces;
using BookingCare.Shared.Cache.Extensions; // Add Redis Cache support
using BookingCare.Shared.Common.Extensions;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Shared.EventBus.Events; // 🎯 Add EventBus events
using BookingCare.Shared.EventBus.Extensions; // 🎯 Add EventBus extensions
using BookingCare.Shared.FileUpload.Extensions;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel with security best practices
builder.WebHost.ConfigureSecureKestrel(builder.Configuration, builder.Environment, "communication");

// Add common services using ProgramExtensions
builder.Services.AddCommonControllers();
builder.Services.AddGrpc();

// BẮT BUỘC: Add API versioning support
builder.Services.AddApiVersioningSupport();

// Add common Swagger configuration using ProgramExtensions
builder.Services.AddCommonSwagger("Communication");

// === REDIS CACHE INTEGRATION ===
// Add Redis caching support for user data
builder.Services.AddRedisCache(builder.Configuration);

// === EVENT BUS INTEGRATION ===
// 🎯 Add RabbitMQ EventBus for user cache invalidation
builder.Services.AddRabbitMQEventBus(builder.Configuration, "communication-service-queue");

// 🎯 Register event handlers for cache invalidation
builder.Services.AddIntegrationEventHandler<UserProfileUpdatedEventHandler>();
builder.Services.AddIntegrationEventHandler<DoctorProfileUpdatedEventHandler>();

// === gRPC CLIENT INTEGRATION ===
// Add Auth Service gRPC client for account details
builder
    .Services.AddGrpcClient<AuthService.AuthServiceClient>(options =>
    {
        var authServiceUrl =
            builder.Configuration.GetValue<string>("GrpcServices:AuthService:Url")
            ?? "http://localhost:6103";
        options.Address = new Uri(authServiceUrl);
    })
    .ConfigurePrimaryHttpMessageHandler(
        () =>
            new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    // In production, use default certificate validation (secure)
                    if (!builder.Environment.IsDevelopment())
                    {
                        return errors == System.Net.Security.SslPolicyErrors.None;
                    }

                    // In development, allow self-signed certificates but still validate hostname
                    if (errors == System.Net.Security.SslPolicyErrors.None)
                    {
                        return true; // Valid certificate
                    }

                    // Allow only self-signed certificate errors in development
                    var allowedErrors = System
                        .Net
                        .Security
                        .SslPolicyErrors
                        .RemoteCertificateChainErrors;
                    if ((errors & ~allowedErrors) != System.Net.Security.SslPolicyErrors.None)
                    {
                        return false; // Other errors like name mismatch are not allowed
                    }

                    // Additional validation for development: check if it's actually a self-signed cert
                    if (cert != null && chain != null)
                    {
                        return chain.ChainStatus.All(status =>
                            status.Status
                                == System
                                    .Security
                                    .Cryptography
                                    .X509Certificates
                                    .X509ChainStatusFlags
                                    .UntrustedRoot
                            || status.Status
                                == System
                                    .Security
                                    .Cryptography
                                    .X509Certificates
                                    .X509ChainStatusFlags
                                    .PartialChain
                        );
                    }

                    return false;
                },
            }
    );

// Register participant enrichment service
builder.Services.AddScoped<IParticipantEnrichmentService, ParticipantEnrichmentService>();

// Add SignalR
builder.Services.AddSignalR(options =>
{
    // Configure SignalR options
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

// Add CORS for SignalR
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "SignalRCorsPolicy",
        policy =>
        {
            policy
                .WithOrigins("http://localhost:5173", "https://localhost:5173") // Add your frontend URLs
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    );
});

// Add MongoDB configuration
builder.Services.AddMongoDb(builder.Configuration);

// === S3-ONLY FILE UPLOAD CONFIGURATION ===
// Add AWS S3 + CloudFront file upload service
builder.Services.AddS3FileUpload(builder.Configuration);

// Configure file upload settings
builder.Services.Configure<FileUploadConfiguration>(
    builder.Configuration.GetSection(FileUploadConfiguration.SectionName)
);

// Register S3-only file upload service (formerly hybrid, now S3-only)
builder.Services.AddScoped<IHybridFileUploadService, HybridFileUploadService>();

// Register wrapper for backward compatibility (IMPORTANT)
builder.Services.AddScoped<
    BookingCare.Services.Communication.Services.Interfaces.IFileUploadService,
    FileUploadServiceWrapper
>();

// Add health checks
builder.Services.AddHealthChecks();

// Add global exception handling
builder.Services.AddGlobalExceptionHandling();

var app = builder.Build();

// Initialize MongoDB indexes
await app.Services.InitializeMongoDbAsync();

// Use global exception handling (early in pipeline)
app.UseGlobalExceptionHandling();

// Use CORS before other middleware
app.UseCors("SignalRCorsPolicy");

// === EVENT BUS MIDDLEWARE ===
// 🎯 Configure EventBus and register event handlers
app.UseEventBus(eventBus =>
{
    // Register UserProfileUpdatedEvent handlers for both User and Doctor profile updates
    eventBus.Subscribe<UserProfileUpdatedEvent, UserProfileUpdatedEventHandler>();
    eventBus.Subscribe<UserProfileUpdatedEvent, DoctorProfileUpdatedEventHandler>();
});

// Configure the HTTP request pipeline using ProgramExtensions
app.UseCommonSwaggerUI("Communication");

app.UseRouting();

// Add health check endpoint
app.MapHealthChecks("/health");

app.MapControllers();

// Map SignalR Hub
app.MapHub<ChatHub>("/chatHub");

// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();

// Add common health check endpoint using ProgramExtensions
app.MapCommonHealthCheck("Communication");

app.Run();
