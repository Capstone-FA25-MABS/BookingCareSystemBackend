using BookingCare.Services.Communication.Services;
using BookingCare.Services.Communication.Extensions;
using BookingCare.Services.Communication.Hubs;
using BookingCare.Services.Communication.Services.Interfaces;
using BookingCare.Services.Communication.Services.Implementations;
using BookingCare.Services.Communication.Configuration;
using BookingCare.Shared.Common.Extensions;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Shared.FileUpload.Extensions;


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
    options.AddPolicy("SignalRCorsPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://localhost:5173") // Add your frontend URLs
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add MongoDB configuration
builder.Services.AddMongoDb(builder.Configuration);

// === S3-ONLY FILE UPLOAD CONFIGURATION ===
// Add AWS S3 + CloudFront file upload service
builder.Services.AddS3FileUpload(builder.Configuration);

// Configure file upload settings
builder.Services.Configure<FileUploadConfiguration>(
    builder.Configuration.GetSection(FileUploadConfiguration.SectionName));

// Register S3-only file upload service (formerly hybrid, now S3-only)
builder.Services.AddScoped<IHybridFileUploadService, HybridFileUploadService>();

// Register wrapper for backward compatibility (IMPORTANT)
builder.Services.AddScoped<BookingCare.Services.Communication.Services.Interfaces.IFileUploadService, FileUploadServiceWrapper>();

// Add health checks
builder.Services.AddHealthChecks();

// Add global exception handling
builder.Services.AddGlobalExceptionHandling();

var app = builder.Build();

// Initialize MongoDB indexes
// await app.Services.InitializeMongoDbAsync();

// Use global exception handling (early in pipeline)
app.UseGlobalExceptionHandling();

// Use CORS before other middleware
app.UseCors("SignalRCorsPolicy");

// Configure the HTTP request pipeline using ProgramExtensions
app.UseCommonSwaggerUI("Communication");

app.UseRouting();

// Add authentication and authorization middleware if needed
// app.UseAuthentication();
// app.UseAuthorization();

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
