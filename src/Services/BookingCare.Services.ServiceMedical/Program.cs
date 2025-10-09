using BookingCare.Services.ServiceMedical.Data;
using BookingCare.Services.ServiceMedical.Mappings;
using BookingCare.Services.ServiceMedical.Repositories.Implementations;
using BookingCare.Services.ServiceMedical.Repositories.Interfaces;
using BookingCare.Services.ServiceMedical.Services.Grpc;
using BookingCare.Services.ServiceMedical.Services.Implementations;
using BookingCare.Services.ServiceMedical.Services.Interfaces;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using BookingCare.Shared.Common.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;

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
builder.Services.AddScoped<IServiceMedicalService, ServiceMedicalService>();
builder.Services.AddScoped<IHospitalServiceClient, HospitalServiceClient>();

// Add HttpClient for Hospital Service
builder.Services.AddHttpClient<IHospitalServiceClient, HospitalServiceClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
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
app.MapControllers();

// Configure gRPC services
// Configure the HTTP request pipeline.
app.MapGrpcService<ServiceMedicalGrpcService>();
app.MapGet("/", () => "BookingCare Service Medical Service is running...");

await app.RunAsync();
