using BookingCare.Services.Review.Extensions;
using BookingCare.Services.Review.Data;
using BookingCare.Services.Review.Mappings;
using BookingCare.Services.Review.Validators;
using BookingCare.Services.Review.Grpc.Services;
using BookingCare.Services.Review.Filters;
using BookingCare.Shared.Common.Extensions;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Services.Auth.Protos;
using FluentValidation;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Mvc;

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
builder.Services.AddControllers(options =>
{
    // Suppress automatic model state validation since we use FluentValidation
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

builder.Services.AddGrpc();
builder.Services.AddEndpointsApiExplorer();

// Add API versioning support
builder.Services.AddApiVersioningSupport();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1.0", new() { Title = "BookingCare Review API", Version = "v1.0" });
});

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
var authServiceAddress = builder.Configuration.GetSection("Services:Auth:GrpcUrl").Value ?? "http://localhost:6013";
builder.Services.AddGrpcClient<AuthService.AuthServiceClient>(options =>
{
    options.Address = new Uri(authServiceAddress);
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1.0/swagger.json", "Review Service V1.0");
        c.RoutePrefix = "swagger";
    });
}

// Add global exception handling early in pipeline
app.UseGlobalExceptionHandling();

app.UseRouting();
app.MapControllers();

// Configure gRPC services
app.MapGrpcService<ReviewGrpcService>();

// Health check endpoint
app.MapGet("/", () => "BookingCare Review Service is running...");

app.Run();
