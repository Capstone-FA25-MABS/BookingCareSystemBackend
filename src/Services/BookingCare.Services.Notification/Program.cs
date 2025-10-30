using BookingCare.Services.Notification.Services;
using BookingCare.Shared.EventBus.Extensions;
using BookingCare.Shared.EventBus.Events;
using BookingCare.Services.Notification.Handlers;
using BookingCare.Services.Notification.Utils.Email;
using BookingCare.Services.Notification.Utils.SMS;
using BookingCare.Services.Notification.Utils.OTP;
using BookingCare.Services.Notification.Setting;
using BookingCare.Services.Auth.Protos;
using BookingCare.Shared.Common.Extensions;
using BookingCare.Services.Notification.Repositories.Implementations;
using BookingCare.Services.Notification.Repositories.Interfaces;
using BookingCare.Services.Notification.Services.Interfaces;
using BookingCare.Services.Notification.Services.Grpc;
using BookingCare.Shared.Cache.Extensions;
using BookingCare.Shared.Common.Versioning;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel with security best practices
builder.WebHost.ConfigureSecureKestrel(builder.Configuration, builder.Environment, "notification");

// Add controllers and Swagger
builder.Services.AddCommonControllers();
builder.Services.AddCommonSwagger("Notification");

builder.Services.AddGrpc();

// Email settings and service
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddSingleton<EmailService>();

// Add Redis cache using shared cache service
builder.Services.AddRedisCache(builder.Configuration);

builder.Services.AddScoped<ManageOtp>();

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
builder.Logging.AddCommonLogging();

// EventBus
builder.Services.AddRabbitMQEventBus(builder.Configuration, "notification-service-queue");
builder.Services.AddIntegrationEventHandler<NotificationSendEventHandler>();
builder.Services.AddIntegrationEventHandler<AppointmentRefundRequestedEventHandler>();
builder.Services.AddIntegrationEventHandler<AppointmentNoRefundNotificationEventHandler>();
builder.Services.AddIntegrationEventHandler<AppointmentCancelledWithOptionsNotificationEventHandler>();
builder.Services.AddIntegrationEventHandler<RefundHistoryCompletedEventHandler>();
builder.Services.AddIntegrationEventHandler<RefundHistoryBankIssueReportedEventHandler>();
builder.Services.AddIntegrationEventHandler<AppointmentBookingSuccessNotificationEventHandler>();
builder.Services.AddIntegrationEventHandler<DoctorCredentialsGeneratedEventHandler>();
builder.Services.AddIntegrationEventHandler<HospitalRegistrationSubmittedEventHandler>();
builder.Services.AddIntegrationEventHandler<HospitalRegistrationStatusUpdatedEventHandler>();
builder.Services.AddIntegrationEventHandler<HospitalAccountCreatedEventHandler>();

// gRPC client for Auth service
builder.Services.AddGrpcClient<AuthService.AuthServiceClient>(o =>
{
    var authServiceUrl = builder.Configuration.GetValue<string>("AuthService:GrpcUrl") ?? "http://localhost:6013";
    o.Address = new Uri(authServiceUrl);
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseCommonSwaggerUI("Notification");
app.UseGlobalExceptionHandling(); // Assuming this is already added via AddGlobalExceptionHandling
app.UseStandardAuthPipeline();

app.MapControllers();

// Map gRPC services
app.MapGrpcService<OtpGrpcService>();

// Map health check endpoint
app.MapCommonHealthCheck("Notification");

// Subscribe to events
app.UseEventBus(eventBus =>
{
    eventBus.Subscribe<NotificationSendEvent, NotificationSendEventHandler>();
    eventBus.Subscribe<AppointmentRefundRequestedIntegrationEvent, AppointmentRefundRequestedEventHandler>();
    eventBus.Subscribe<AppointmentNoRefundNotificationEvent, AppointmentNoRefundNotificationEventHandler>();
    eventBus.Subscribe<AppointmentCancelledWithOptionsNotificationEvent, AppointmentCancelledWithOptionsNotificationEventHandler>();
    eventBus.Subscribe<RefundHistoryCompletedIntegrationEvent, RefundHistoryCompletedEventHandler>();
    eventBus.Subscribe<RefundHistoryBankIssueReportedIntegrationEvent, RefundHistoryBankIssueReportedEventHandler>();

    // Subscribe to appointment booking success notifications for email sending
    eventBus.Subscribe<AppointmentBookingSuccessNotificationEvent, AppointmentBookingSuccessNotificationEventHandler>();

    // Subscribe to doctor credentials generated event for sending login credentials
    eventBus.Subscribe<DoctorCredentialsGeneratedEvent, DoctorCredentialsGeneratedEventHandler>();

    // Subscribe to hospital registration events for sending confirmation/status emails
    eventBus.Subscribe<HospitalRegistrationSubmittedEvent, HospitalRegistrationSubmittedEventHandler>();
    eventBus.Subscribe<HospitalRegistrationStatusUpdatedEvent, HospitalRegistrationStatusUpdatedEventHandler>();

    // Subscribe to hospital account created event for sending credentials email
    eventBus.Subscribe<HospitalAccountCreatedEvent, HospitalAccountCreatedEventHandler>();
});

await app.RunAsync();