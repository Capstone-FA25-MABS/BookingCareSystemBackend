using BookingCare.Services.Favorites.Extensions;
using BookingCare.Services.Favorites.Services.Grpc;
using BookingCare.Shared.Common.Extensions;
using BookingCare.Shared.Common.Versioning;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel with security best practices
builder.WebHost.ConfigureSecureKestrel(builder.Configuration, builder.Environment, "favorites");

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddEndpointsApiExplorer();

// BẮT BUỘC: Add API versioning support
builder.Services.AddApiVersioningSupport();

// Swagger configuration with versioning
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1.0", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "BookingCare Favorites API",
        Version = "v1.0",
        Description = "API for managing user favorites for doctors"
    });
});

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

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1.0/swagger.json", "BookingCare Favorites Service V1.0");
        c.RoutePrefix = string.Empty; // Set Swagger UI at app root
    });
}

// Use global exception handling early in pipeline
app.UseGlobalExceptionHandling();

app.UseRouting();
app.MapControllers();

// Map gRPC services
app.MapGrpcService<FavoritesGrpcService>();

// Default route
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
