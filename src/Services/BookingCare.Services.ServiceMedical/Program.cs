using BookingCare.Services.ServiceMedical.Data;
using BookingCare.Services.ServiceMedical.Mappings;
using BookingCare.Services.ServiceMedical.Repositories.Implementations;
using BookingCare.Services.ServiceMedical.Repositories.Interfaces;
using BookingCare.Services.ServiceMedical.Services.Grpc;
using BookingCare.Services.ServiceMedical.Services.Implementations;
using BookingCare.Services.ServiceMedical.Services.Interfaces;
using BookingCare.Shared.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using BookingCare.Shared.Common.Extensions;
using BookingCare.Shared.FileUpload.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using BookingCare.Services.Review.Grpc;

// Enable HTTP/2 without TLS for gRPC (development only)
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel with security best practices
builder.WebHost.ConfigureSecureKestrel(builder.Configuration, builder.Environment, "servicemedical");

// Add Entity Framework
builder.Services.AddDbContext<ServiceMedicalDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(ServiceMedicalMappingProfile));

// Add Repositories
builder.Services.AddScoped<IServiceCategoryRepository, ServiceCategoryRepository>();
builder.Services.AddScoped<IServiceRepository, ServiceRepository>();

// Add Services
builder.Services.AddScoped<ILocationApiService, BookingCare.Shared.Common.Services.LocationApiService>();
builder.Services.AddScoped<IServiceMedicalService, ServiceMedicalService>();
builder.Services.AddScoped<IHospitalService, HospitalService>();

// Add Hospital gRPC clients
var hospitalAddress = builder.Configuration.GetSection("GrpcClients:Hospital:Address").Value ?? "http://localhost:6104";
builder.Services.AddGrpcClient<BookingCare.Services.Hospital.HospitalService.HospitalServiceClient>(options =>
{
    options.Address = new Uri(hospitalAddress);
});
builder.Services.AddGrpcClient<BookingCare.Services.Hospital.SubscriptionUsageGrpc.SubscriptionUsageGrpcClient>(options =>
{
    options.Address = new Uri(hospitalAddress);
});

// Add Review gRPC client
var reviewAddress = builder.Configuration.GetSection("GrpcClients:Review:Address").Value ?? "http://localhost:6112";
builder.Services.AddGrpcClient<ReviewService.ReviewServiceClient>(options =>
{
    options.Address = new Uri(reviewAddress);
});

// Aggregate gRPC clients for ServiceMedicalService to keep constructor focused
builder.Services.AddScoped<ServiceMedicalService.ServiceMedicalGrpcClients>(sp =>
{
    var subscriptionUsageClient = sp.GetRequiredService<BookingCare.Services.Hospital.SubscriptionUsageGrpc.SubscriptionUsageGrpcClient>();
    var reviewServiceClient = sp.GetRequiredService<ReviewService.ReviewServiceClient>();
    return new ServiceMedicalService.ServiceMedicalGrpcClients(subscriptionUsageClient, reviewServiceClient);
});

// Add API Versioning
builder.Services.AddApiVersioning(opt =>
{
    opt.DefaultApiVersion = new ApiVersion(1, 0);
    opt.AssumeDefaultVersionWhenUnspecified = true;
    opt.ApiVersionReader = ApiVersionReader.Combine(
        new HeaderApiVersionReader("X-Version"),
        new QueryStringApiVersionReader("version"),
        new UrlSegmentApiVersionReader()
    );
});

builder.Services.AddVersionedApiExplorer(setup =>
{
    setup.GroupNameFormat = "'v'VVV";
    setup.SubstituteApiVersionInUrl = true;
});

// Add S3 File Upload services
builder.Services.AddS3FileUpload(builder.Configuration);

// Add JWT Authentication & Authorization (including dynamic role policies)
builder.Services.AddJwtAuthAndAuthorization(builder.Configuration, builder.Environment);

builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "BookingCare Medical Service API",
        Version = "v1",
        Description = "API for managing medical services and service categories"
    });

    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, xmlFile);
    if (System.IO.File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BookingCare Medical Service API v1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
    });
}

app.UseRouting();

// Standard auth pipeline (Authentication, Authorization, AutoToken, etc.)
app.UseStandardAuthPipeline();

app.MapControllers();

// Configure gRPC services
app.MapGrpcService<ServiceMedicalGrpcService>();
app.MapGet("/", () => "BookingCare Service Medical Service is running...");

await app.RunAsync();