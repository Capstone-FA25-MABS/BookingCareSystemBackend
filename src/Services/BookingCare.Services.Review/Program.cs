using BookingCare.Services.Review.Services;
using BookingCare.Services.Review.Extensions;
using BookingCare.Services.Review.Data;
using BookingCare.Services.Review.Mappings;
using BookingCare.Services.Review.Validators;
using BookingCare.Shared.Common.Extensions;
using FluentValidation;
using Microsoft.AspNetCore.Server.Kestrel.Core;

// Enable HTTP/2 without TLS for gRPC (development only)
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(6012, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });
    options.ListenAnyIP(6022, listenOptions =>
    {
        // listenOptions.UseHttps();
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add global exception handling
builder.Services.AddGlobalExceptionHandling();

// Add MongoDB configuration
builder.Services.AddMongoDb(builder.Configuration);

// Add Review service dependencies
builder.Services.AddReviewServices();

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(ReviewMappingProfile));

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateReviewRequestValidator>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Add global exception handling early in pipeline
app.UseGlobalExceptionHandling();

app.UseRouting();
app.MapControllers();

// Configure gRPC services
app.MapGrpcService<GreeterService>();

// Health check endpoint
app.MapGet("/", () => "BookingCare Review Service is running...");

// Initialize MongoDB indexes on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var indexService = scope.ServiceProvider.GetRequiredService<IIndexInitializationService>();
        await indexService.InitializeIndexesAsync();
        
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("MongoDB indexes initialized successfully");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to initialize MongoDB indexes");
        // Don't stop the application if index initialization fails
    }
}

app.Run();
