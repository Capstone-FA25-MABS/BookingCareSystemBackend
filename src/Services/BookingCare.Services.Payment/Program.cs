using BookingCare.Services.Payment.Data;
using BookingCare.Services.Payment.Services.Interfaces;
using BookingCare.Services.Payment.Services.Implementations;
using BookingCare.Services.Payment.Services.BackgroundServices;
using BookingCare.Services.Payment.Repositories.Interfaces;
using BookingCare.Services.Payment.Repositories.Implementations;
using BookingCare.Services.Payment.Mappings;
using BookingCare.Services.Payment.Validators;
using BookingCare.Services.Payment.Models.Configurations;
using BookingCare.Services.Payment.Handlers;
using BookingCare.Shared.Common.Extensions;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Shared.Common.AppRouting;
using BookingCare.Shared.EventBus.Extensions;
using BookingCare.Shared.EventBus.Events;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.SwaggerGen;
using FluentValidation;
using BookingCare.Services.Payment.Services.Grpc;

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

// Bind Frontend options for base URL resolution
builder.Services.Configure<FrontendOptions>(builder.Configuration.GetSection(FrontendOptions.SectionName));

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
builder.Services.AddScoped<IPaymentValidationService, PaymentValidationService>();
builder.Services.AddScoped<IAppointmentDetailsService, AppointmentDetailsService>();

// Add background services
builder.Services.AddHostedService<PayOSMappingCleanupService>();
builder.Services.AddHostedService<RefundHistoryProcessingService>();
builder.Services.AddHostedService<PendingPaymentCleanupService>();

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(PaymentMappingProfile));

// Add validators
builder.Services.AddValidatorsFromAssemblyContaining<CreatePaymentRequestValidator>();

// Add EventBus for message queue integration
builder.Services.AddRabbitMQEventBus(builder.Configuration, "payment-service-queue");
builder.Services.AddIntegrationEventHandler<AppointmentCancelledEventHandler>();
builder.Services.AddIntegrationEventHandler<BankAccountCreatedEventHandler>();

// Add gRPC client for User Service (to fetch patient info)
// Using UserService from Appointment Service project reference
builder.Services.AddGrpcClient<BookingCare.Services.User.Protos.UserService.UserServiceClient>(o =>
{
    var userServiceUrl = builder.Configuration.GetSection("Services:User").GetValue<string>("GrpcUrl") ?? "http://localhost:6116";
    o.Address = new Uri(userServiceUrl);
});

// Add gRPC client for Appointment Service (to get doctorId from appointmentId for payment failure redirect)
builder.Services.AddGrpcClient<BookingCare.Services.Appointment.Protos.AppointmentService.AppointmentServiceClient>(o =>
{
    var appointmentServiceUrl = builder.Configuration.GetSection("Services:Appointment").GetValue<string>("GrpcUrl") ?? "http://localhost:6102";
    o.Address = new Uri(appointmentServiceUrl);
});

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
app.MapGrpcService<PaymentGrpcService>();

// Map common health check
app.MapCommonHealthCheck("Payment Service");

// Subscribe to appointment cancellation events
app.UseEventBus(eventBus =>
{
    eventBus.Subscribe<AppointmentCancelledIntegrationEvent, AppointmentCancelledEventHandler>();
    eventBus.Subscribe<BankAccountCreatedIntegrationEvent, BankAccountCreatedEventHandler>();
});

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
