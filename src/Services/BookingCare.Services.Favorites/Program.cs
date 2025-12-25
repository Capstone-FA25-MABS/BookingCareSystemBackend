using BookingCare.Services.Favorites.Extensions;
using BookingCare.Services.Favorites.Services.Grpc;
using BookingCare.Shared.Common.Extensions;
using BookingCare.Shared.Common.Interfaces;
using BookingCare.Shared.Common.Versioning;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel with security best practices
builder.WebHost.ConfigureSecureKestrel(builder.Configuration, builder.Environment, "favorites");

// Add common services using ProgramExtensions
builder.Services.AddCommonControllers();
builder.Services.AddGrpc();

// BẮT BUỘC: Add API versioning support
builder.Services.AddApiVersioningSupport();

// Add JWT Authentication & Authorization following Auth service pattern
builder.Services.AddJwtAuthAndAuthorization(builder.Configuration, builder.Environment);

// Add common Swagger configuration using ProgramExtensions
builder.Services.AddCommonSwagger("Favorites");

// Add MongoDB services - using configured MongoDbSettings
builder.Services.AddMongoDb(builder.Configuration);

// Add application services
builder.Services.AddApplicationServices();

// Add AutoMapper
builder.Services.AddAutoMapperServices();

// Add FluentValidation
builder.Services.AddValidationServices();

// Add global exception handling
builder.Services.AddGlobalExceptionHandling();


// Add BookingCare metrics
builder.Services.AddBookingCareMetrics("favorites-service");
var app = builder.Build();

// Enable metrics if configured
var enableMetrics = Environment.GetEnvironmentVariable("ENABLE_PROMETHEUS_METRICS") == "true";
app.UseBookingCareMetrics(enableMetrics);

// Configure the HTTP request pipeline using ProgramExtensions
app.UseCommonSwaggerUI("Favorites");

// Use global exception handling early in pipeline
app.UseGlobalExceptionHandling();

// Use standard authentication pipeline (includes UseRouting, UseAuthentication, UseAuthorization)
app.UseStandardAuthPipeline();

app.MapControllers();

// Map gRPC services
app.MapGrpcService<FavoritesGrpcService>();

// Add common health check endpoint using ProgramExtensions
app.MapCommonHealthCheck("Favorites");

// Default route (keeping existing functionality)
app.MapGet("/", () => "BookingCare Favorites Service is running...");

// Initialize database
try
{
    await app.Services.InitializeDatabaseAsync();
    Console.WriteLine("MongoDB database initialized successfully with indexes");
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to initialize MongoDB database: {ex.Message}");
    // Don't exit the application, just log the error
}

app.Run();
