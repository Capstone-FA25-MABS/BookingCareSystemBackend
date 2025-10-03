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

builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.MapControllers();

// Configure gRPC services
app.MapGrpcService<ServiceMedicalGrpcService>();
app.MapGet("/", () => "BookingCare Service Medical Service is running...");

app.Run();
