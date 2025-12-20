using BookingCare.Services.AI.Configuration;
using BookingCare.Services.AI.Data;
using BookingCare.Services.AI.Helpers;
using BookingCare.Services.AI.Models.Entities;
using BookingCare.Services.AI.Services;
using BookingCare.Services.AI.Services.Implementations;
using BookingCare.Services.AI.Services.Interfaces;
using BookingCare.Services.AI.Workflows;
using BookingCare.Services.Doctor.Protos;
using BookingCare.Shared.Common.Extensions;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Shared.EventBus.Extensions;
using BookingCare.Shared.FileUpload.Extensions;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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

// Configure Gemini common settings
builder.Services.Configure<GeminiConfiguration>(builder.Configuration.GetSection("Gemini"));

// Configure Gemini Services (each service has its own API key)
builder.Services.Configure<GeminiServicesConfiguration>(builder.Configuration.GetSection("GeminiServices"));

// Configure Groq settings
builder.Services.Configure<GroqConfiguration>(builder.Configuration.GetSection("Groq"));

// Configure Groq Services (each service has its own API key)
builder.Services.Configure<GroqServicesConfiguration>(builder.Configuration.GetSection("GroqServices"));

// Configure AILabTools for Dermatology Analysis
builder.Services.Configure<AILabToolsConfiguration>(builder.Configuration.GetSection("AILabTools"));



// Register GeminiApiHelper (shared helper for all Gemini API calls)
builder.Services.AddHttpClient<GeminiApiHelper>();

// Register GroqApiHelper (shared helper for all Groq API calls)
// Use scoped registration with HttpClient from IHttpClientFactory
builder.Services.AddHttpClient();
builder.Services.AddScoped<GroqApiHelper>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<GroqApiHelper>>();
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient();
    var config = sp.GetRequiredService<IOptions<GroqConfiguration>>();
    return new GroqApiHelper(logger, httpClient, config);
});

// Register Pexels API Helper for fetching food and exercise images
builder.Services.AddScoped<PexelsApiHelper>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<PexelsApiHelper>>();
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient();
    var configuration = sp.GetRequiredService<IConfiguration>();
    return new PexelsApiHelper(logger, httpClient, configuration);
});

// Register AI Service for medical summary generation
builder.Services.AddScoped<IAIService, AIService>();
builder.Services.AddScoped<IAiInsightsService, AiInsightsService>();

// Register gRPC clients
var doctorGrpcAddress =
    builder.Configuration["GrpcClients:Doctor:Address"]
    ?? builder.Configuration["Services:Doctor:GrpcUrl"]
    ?? "http://localhost:6108";
builder.Services.AddGrpcClient<DoctorService.DoctorServiceClient>(options =>
{
    options.Address = new Uri(doctorGrpcAddress);
});

var hospitalGrpcAddress =
    builder.Configuration["GrpcClients:Hospital:Address"]
    ?? builder.Configuration["Services:Hospital:GrpcUrl"]
    ?? "http://localhost:6104";
builder.Services.AddGrpcClient<BookingCare.Services.Hospital.HospitalService.HospitalServiceClient>(
    options =>
    {
        options.Address = new Uri(hospitalGrpcAddress);
    }
);

// Add User service gRPC client for nutrition service
var userGrpcAddress =
    builder.Configuration["GrpcClients:User:Address"]
    ?? builder.Configuration["Services:User:GrpcUrl"]
    ?? "http://localhost:6101";
builder.Services.AddGrpcClient<BookingCare.Services.User.Protos.UserService.UserServiceClient>(
    options =>
    {
        options.Address = new Uri(userGrpcAddress);
    }
);

// Register Helper Services
builder.Services.AddScoped<RecommendationHelper>();
builder.Services.AddScoped<FileUploadHelper>();

// Register Cache Services for Context-Aware Question Caching
builder.Services.AddScoped<IContextKeywordExtractor, ContextKeywordExtractor>();
builder.Services.AddScoped<IQuestionCacheService, QuestionCacheService>();

// Register Cache Services for Lab Result Analysis
builder.Services.AddScoped<ILabResultKeywordExtractor, LabResultKeywordExtractor>();
builder.Services.AddScoped<ILabResultCacheService, LabResultCacheService>();

// Register Cache Services for Dermatology Analysis
builder.Services.AddScoped<IDermatologyCacheService, DermatologyCacheService>();

// Register AILabTools API Key Service
builder.Services.AddScoped<IAILabToolsApiKeyService, AILabToolsApiKeyService>();

// Register Conversation Session Service
builder.Services.AddScoped<IConversationSessionService, ConversationSessionService>();

// Register Symptom Analysis Service
builder.Services.AddScoped<ISymptomAnalysisService, SymptomAnalysisService>();

// Register Lab Result Analysis Service
builder.Services.AddScoped<ILabResultAnalysisService, LabResultAnalysisService>();

// Register Dermatology Analysis Service
builder.Services.AddHttpClient<IDermatologyAnalysisService, DermatologyAnalysisService>();

// Register Gemini Transcription Service
builder.Services.AddScoped<IGeminiTranscriptionService, GeminiTranscriptionService>();

// Register Audio Transcription Workflow
builder.Services.AddScoped<IAudioTranscriptionWorkflow, AudioTranscriptionWorkflow>();

// Register Nutrition Services
builder.Services.AddScoped<HealthMetricsCalculator>();
builder.Services.AddScoped<INutritionService, NutritionService>();
builder.Services.AddScoped<INutritionConversationService, NutritionConversationService>();

// Add S3 File Upload services
builder.Services.AddS3FileUpload(builder.Configuration);

// Add Memory Cache for token caching
builder.Services.AddMemoryCache();

// Add RabbitMQ Event Bus for nutrition notifications
builder.Services.AddRabbitMQEventBus(builder.Configuration, "ai-service-queue");

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

app.MapGet("/", () => "Medcure AI Service is running...");

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

