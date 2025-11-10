using Microsoft.EntityFrameworkCore;
using BookingCare.Shared.Common.Extensions;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Services.AI.Configuration;
using BookingCare.Services.AI.Services.Interfaces;
using BookingCare.Services.AI.Services.Implementations;
using BookingCare.Services.AI.Data;
using BookingCare.Services.Doctor.Protos;
using Grpc.Net.Client;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel with security best practices
builder.WebHost.ConfigureSecureKestrel(builder.Configuration, builder.Environment, "ai");

// Add API Versioning
builder.Services.AddApiVersioningSupport();

builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database configuration
builder.Services.AddDbContext<AiDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString);
});

// Configure Gemini Settings
builder.Services.Configure<GeminiSettings>(builder.Configuration.GetSection("Gemini"));

// Add HttpClient for external service calls
builder.Services.AddHttpClient("BookingCareServices", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Register Gemini Service
builder.Services.AddHttpClient<IGeminiService, GeminiService>();

// Register gRPC clients
var doctorGrpcAddress = builder.Configuration["GrpcClients:Doctor:Address"] ?? builder.Configuration["Services:Doctor:GrpcUrl"] ?? "http://localhost:6108";
builder.Services.AddGrpcClient<DoctorService.DoctorServiceClient>(options =>
{
    options.Address = new Uri(doctorGrpcAddress);
});

var hospitalGrpcAddress = builder.Configuration["GrpcClients:Hospital:Address"] ?? builder.Configuration["Services:Hospital:GrpcUrl"] ?? "http://localhost:6104";
builder.Services.AddGrpcClient<BookingCare.Services.Hospital.HospitalService.HospitalServiceClient>(options =>
{
    options.Address = new Uri(hospitalGrpcAddress);
});

// Register Conversation Session Service
builder.Services.AddScoped<IConversationSessionService, ConversationSessionService>();

// Register Symptom Analysis Service
builder.Services.AddScoped<ISymptomAnalysisService, SymptomAnalysisService>();

// Add JWT Authentication and Authorization using centralized configuration
// This includes: JWT auth, authorization policies, and AutoToken middleware
builder.Services.AddJwtAuthAndAuthorization();

// Add global exception handling
builder.Services.AddGlobalExceptionHandling();

// Add logging
builder.Logging.AddCommonLogging();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Use standard authentication pipeline
// This includes: AutoToken middleware, Routing, Authentication, Authorization
app.UseGlobalExceptionHandling();
app.UseStandardAuthPipeline();

app.MapControllers();

app.MapGet("/", () => "BookingCare AI Service is running...");

// Database migration and seeding
using var scope = app.Services.CreateScope();
try
{
    var context = scope.ServiceProvider.GetRequiredService<AiDbContext>();
    await context.Database.MigrateAsync();
    app.Logger.LogInformation("AI Service database migrated successfully");
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "An error occurred while migrating database");
}

app.Run();
