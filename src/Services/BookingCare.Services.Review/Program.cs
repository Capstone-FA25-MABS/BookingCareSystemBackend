using BookingCare.Services.Review.Extensions;
using BookingCare.Services.Review.Mappings;
using BookingCare.Services.Review.Validators;
using BookingCare.Services.Review.Grpc.Services;
using BookingCare.Services.Review.Filters;
using BookingCare.Shared.Common.Extensions;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Services.Auth.Protos;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel with security best practices
builder.WebHost.ConfigureSecureKestrel(builder.Configuration, builder.Environment, "review");

// Add services to the container with custom configuration for FluentValidation
builder.Services.AddControllers(options =>
{
    // Suppress automatic model state validation since we use FluentValidation
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

builder.Services.AddGrpc();

// Add API versioning support
builder.Services.AddApiVersioningSupport();

// Add JWT Authentication & Authorization following Auth service pattern
builder.Services.AddJwtAuthAndAuthorization(builder.Configuration, builder.Environment);

// Add common Swagger configuration using ProgramExtensions
builder.Services.AddCommonSwagger("Review");

// Add global exception handling
builder.Services.AddGlobalExceptionHandling();

// Add MongoDB configuration
builder.Services.AddMongoDb(builder.Configuration);

// Add Review service dependencies
builder.Services.AddReviewServices();

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(ReviewMappingProfile));

// Add Model Binding Error Filter (must be first to catch binding errors)
builder.Services.AddModelBindingErrorFilter();

// Add FluentValidation with automatic validation filter
builder.Services.AddValidatorsFromAssemblyContaining<CreateReviewRequestValidator>();
builder.Services.AddValidationFilter();

// Configure FluentValidation options
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    // Disable default model validation behavior since we handle it with FluentValidation
    options.SuppressModelStateInvalidFilter = true;
});

// Configure gRPC clients
var authServiceAddress = builder.Configuration.GetSection("Services:Auth:GrpcUrl").Value ?? "http://localhost:6103";
builder.Services.AddGrpcClient<AuthService.AuthServiceClient>(options =>
{
    options.Address = new Uri(authServiceAddress);
});

// Add UserService gRPC client for optimized patient enrichment
var userServiceAddress = builder.Configuration.GetSection("Services:User:GrpcUrl").Value ?? "http://localhost:6116";
builder.Services.AddGrpcClient<BookingCare.Services.User.Protos.UserService.UserServiceClient>(options =>
{
    options.Address = new Uri(userServiceAddress);
});

// Add AppointmentService gRPC client for appointment history validation
var appointmentServiceAddress = builder.Configuration.GetSection("Services:Appointment:GrpcUrl").Value ?? "http://localhost:6102";
builder.Services.AddGrpcClient<BookingCare.Services.Appointment.Protos.AppointmentService.AppointmentServiceClient>(options =>
{
    options.Address = new Uri(appointmentServiceAddress);
});

var app = builder.Build();

// Configure the HTTP request pipeline using ProgramExtensions
app.UseCommonSwaggerUI("Review");

// Add global exception handling early in pipeline
app.UseGlobalExceptionHandling();

// Use standard authentication pipeline (includes UseRouting, UseAuthentication, UseAuthorization)
app.UseStandardAuthPipeline();

app.MapControllers();

// Configure gRPC services
app.MapGrpcService<ReviewGrpcService>();

// Add common health check endpoint using ProgramExtensions
app.MapCommonHealthCheck("Review");

// Default route (keeping existing functionality)
app.MapGet("/", () => "BookingCare Review Service is running...");

app.Run();
