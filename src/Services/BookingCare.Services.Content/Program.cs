using BookingCare.Services.Content.Data;
using BookingCare.Services.Content.Mappings;
using BookingCare.Services.Content.Repositories;
using BookingCare.Services.Content.Services;
using BookingCare.Services.Content.Validators;
using BookingCare.Services.User.Protos;
using BookingCare.Shared.Common.Extensions;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Shared.FileUpload.Extensions;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel with security best practices
builder.WebHost.ConfigureSecureKestrel(builder.Configuration, builder.Environment, "content");

// Database
builder.Services.AddDbContext<ContentDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ??
                         throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.")));

// Common API configuration (matching other services)
builder.Services.AddCommonControllers();
builder.Services.AddApiVersioningSupport();
builder.Services.AddJwtAuthAndAuthorization(builder.Configuration, builder.Environment);
builder.Services.AddCommonSwagger("Content");
builder.Services.AddGlobalExceptionHandling();
builder.Logging.AddCommonLogging();

// AutoMapper
builder.Services.AddAutoMapper(typeof(HospitalFaqMappingProfile), typeof(BlogMappingProfile));

// FluentValidation (HospitalFaq + Blog)
builder.Services.AddValidatorsFromAssemblyContaining<CreateHospitalFaqRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateBlogRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateBlogCategoryRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();

// File upload (used by BlogsController)
builder.Services.AddS3FileUpload(builder.Configuration);

// Repositories
builder.Services.AddScoped<IHospitalFaqRepository, HospitalFaqRepository>();
builder.Services.AddScoped<IBlogRepository, BlogRepository>();
builder.Services.AddScoped<IBlogCategoryRepository, BlogCategoryRepository>();

// Services
builder.Services.AddScoped<IHospitalFaqService, HospitalFaqService>();
builder.Services.AddScoped<IBlogService, BlogService>();
builder.Services.AddScoped<IBlogCategoryService, BlogCategoryService>();
builder.Services.AddScoped<DatabaseInitializationService>();

// gRPC
builder.Services.AddGrpc();

// gRPC client for User service (Blog author names)
builder.Services.AddGrpcClient<UserService.UserServiceClient>(o =>
{
    var endpoint = builder.Configuration.GetSection("Services:User").GetValue<string>("GrpcUrl")
                   ?? "http://localhost:6116";
    o.Address = new Uri(endpoint);
});

var app = builder.Build();

// HTTP pipeline
app.UseCommonSwaggerUI("Content");

// Initialize database using DatabaseInitializationService
using (var scope = app.Services.CreateScope())
{
    var databaseInitializationService = scope.ServiceProvider.GetRequiredService<DatabaseInitializationService>();
    await databaseInitializationService.InitializeAsync();
}

app.UseGlobalExceptionHandling();
app.UseStandardAuthPipeline();
app.MapControllers();

// Simple health check for now
app.MapCommonHealthCheck("Content");
app.MapGet("/", () => "BookingCare Content Service is running...");

app.Run();
