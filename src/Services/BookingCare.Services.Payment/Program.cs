using BookingCare.Services.Payment.Services;
using BookingCare.Services.Payment.Data;
using BookingCare.Services.Payment.Services.Interfaces;
using BookingCare.Services.Payment.Services.Implementations;
using BookingCare.Services.Payment.Services.BackgroundServices;
using BookingCare.Services.Payment.Repositories.Interfaces;
using BookingCare.Services.Payment.Repositories.Implementations;
using BookingCare.Services.Payment.Mappings;
using BookingCare.Services.Payment.Validators;
using BookingCare.Services.Payment.Models.Configurations;
using BookingCare.Shared.Common.Extensions;
using BookingCare.Shared.Common.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel with security best practices
builder.WebHost.ConfigureSecureKestrel(builder.Configuration, builder.Environment, "payment");

// Add Entity Framework
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ??
        "Server=(local);Database=PaymentDb;Trusted_Connection=True;TrustServerCertificate=True;"));

// Add Payment-specific configurations
builder.Services.Configure<VNPayConfiguration>(builder.Configuration.GetSection("VNPayConfiguration"));
builder.Services.Configure<PayOSConfiguration>(builder.Configuration.GetSection("PayOSConfiguration"));

// Add repositories
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();
builder.Services.AddScoped<IPayOSPaymentMappingRepository, PayOSPaymentMappingRepository>();
builder.Services.AddScoped<IBankAccountRepository, BankAccountRepository>();
builder.Services.AddScoped<IRefundHistoryRepository, RefundHistoryRepository>();

// Add services
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPaymentMethodService, PaymentMethodService>();
builder.Services.AddScoped<IVNPayService, VNPayService>();
builder.Services.AddScoped<IPayOSService, PayOSService>();
builder.Services.AddScoped<IBankAccountService, BankAccountService>();
builder.Services.AddScoped<IRefundHistoryService, RefundHistoryService>();

// Add background services
builder.Services.AddHostedService<PayOSMappingCleanupService>();
builder.Services.AddHostedService<RefundHistoryProcessingService>();

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(PaymentMappingProfile));

// Add validators
builder.Services.AddValidatorsFromAssemblyContaining<CreatePaymentRequestValidator>();

// Add API versioning support
builder.Services.AddApiVersioningSupport();

// Add global exception handling
builder.Services.AddGlobalExceptionHandling();

// Add common services using ProgramExtensions
builder.Services.AddCommonControllers();
builder.Services.AddGrpc();

// Add Swagger with XML documentation support
builder.Services.AddCommonSwagger("Payment Service");
builder.Services.Configure<SwaggerGenOptions>(c =>
{
    // Include XML comments for Payment Service
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});



var app = builder.Build();

// Ensure database is created
await EnsureDatabaseCreated(app);

// Add global exception handling early in pipeline
app.UseGlobalExceptionHandling();



// Use common Swagger UI configuration
app.UseCommonSwaggerUI("Payment Service");

app.MapControllers();
app.MapGrpcService<GreeterService>();

// Map common health check
app.MapCommonHealthCheck("Payment Service");

app.Run();

/// <summary>
/// Ensures the database is created and configured properly
/// </summary>
static async Task EnsureDatabaseCreated(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    try
    {
        await context.Database.EnsureCreatedAsync();
        app.Logger.LogInformation("Database initialized successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred while initializing the database");
    }
}
