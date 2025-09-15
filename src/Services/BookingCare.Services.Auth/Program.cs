using BookingCare.Services.Auth.Data;
using BookingCare.Services.Auth.Models.Entities;
using BookingCare.Services.Auth.Repositories;
using BookingCare.Services.Auth.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using BookingCare.Services.Auth.Mappings;
using System.Text.Json.Serialization;
using BookingCare.Services.Notification.Protos;
using BookingCare.Services.Auth.Utils;
using BookingCare.Shared.EventBus.Extensions;
using BookingCare.Shared.Common.AppRouting;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using BookingCare.Shared.Common.Extensions;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Services.Auth.Providers;

// Enable HTTP/2 without TLS for gRPC (development only)
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    // HTTP endpoint for REST API
    options.ListenAnyIP(6003, listenOptions =>
    {
        //listenOptions.UseHttps();
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });

    // gRPC endpoint
    options.ListenAnyIP(6013, listenOptions =>
    {
        // listenOptions.UseHttps();
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});
// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add DbContext
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity
builder.Services.AddIdentity<AccountEntity, RoleEntity>(options =>
{
    // Cấu hình mật khẩu:
    // - RequireDigit: Yêu cầu phải có ít nhất một chữ số trong mật khẩu.
    options.Password.RequireDigit = true;
    // - RequireLowercase: Yêu cầu phải có ít nhất một ký tự thường.
    options.Password.RequireLowercase = true;
    // - RequireNonAlphanumeric: Yêu cầu phải có ít nhất một ký tự đặc biệt (không phải chữ hoặc số).
    options.Password.RequireNonAlphanumeric = true;
    // - RequireUppercase: Yêu cầu phải có ít nhất một ký tự in hoa.
    options.Password.RequireUppercase = true;
    // - RequiredLength: Độ dài tối thiểu của mật khẩu là 8 ký tự.
    options.Password.RequiredLength = 8;
    // - RequiredUniqueChars: Số lượng ký tự khác nhau tối thiểu trong mật khẩu là 1.
    options.Password.RequiredUniqueChars = 1;

    // Cấu hình khóa tài khoản:
    // - DefaultLockoutTimeSpan: Thời gian khóa tài khoản mặc định là 5 phút khi đăng nhập sai quá số lần cho phép.
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    // - MaxFailedAccessAttempts: Số lần đăng nhập sai tối đa trước khi bị khóa là 5 lần.
    options.Lockout.MaxFailedAccessAttempts = 5;
    // - AllowedForNewUsers: Cho phép áp dụng chính sách khóa tài khoản cho người dùng mới.
    options.Lockout.AllowedForNewUsers = true;

    // Cấu hình người dùng: 
    // - RequireUniqueEmail: Yêu cầu email phải là duy nhất.
    options.User.RequireUniqueEmail = true;

})
.AddEntityFrameworkStores<AuthDbContext>()
.AddDefaultTokenProviders();

// Add HttpContextAccessor for cookie management
builder.Services.AddHttpContextAccessor();

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(AuthMappingProfile));

// Add Repository
builder.Services.AddScoped<IAuthRepository, AuthRepository>();

// Add Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<AuthGrpcService>();
builder.Services.AddScoped<DataInitializationService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<CookieService>();
// Register External Auth Providers
builder.Services.AddHttpClient<GoogleAuthProvider>();
builder.Services.AddHttpClient<FacebookAuthProvider>();
builder.Services.AddScoped<GoogleAuthProvider>();
builder.Services.AddScoped<FacebookAuthProvider>();
builder.Services.AddScoped<IExternalAuthProvider, GoogleAuthProvider>();
builder.Services.AddScoped<IExternalAuthProvider, FacebookAuthProvider>();
builder.Services.AddScoped<ExternalAuthProviderService>();

// Add gRPC client for OTP verification
builder.Services.AddGrpcClient<OtpVerifier.OtpVerifierClient>(o =>
{
    var endpoint = builder.Configuration.GetSection("OtpVerification").GetValue<string>("GrpcEndpoint") ?? "http://localhost:6020";
    o.Address = new Uri(endpoint);
});

// Add global exception handling
builder.Services.AddGlobalExceptionHandling();

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add JWT Authentication and Authorization using centralized configuration
// This includes: JWT auth, authorization, and frontend configuration
builder.Services.AddJwtAuthAndAuthorization();

// Add CORS (restrict to configured frontends)
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontEnd", policy =>
    {
        policy.WithOrigins(allowedOrigins!)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add gRPC
builder.Services.AddGrpc();

builder.Services.AddEndpointsApiExplorer();

// Add API versioning support
builder.Services.AddApiVersioningSupport();

builder.Services.AddSwaggerGen(c =>
{
    // Register common group names to avoid mismatch (some setups produce v1 instead of v1.0)
    c.SwaggerDoc("v1", new() { Title = "BookingCare Auth API", Version = "v1" });
    c.SwaggerDoc("v1.0", new() { Title = "BookingCare Auth API", Version = "v1.0" });
    // Ensure endpoints are included in the correct Swagger doc based on ApiExplorer group name (e.g., v1.0)
    c.DocInclusionPredicate((docName, apiDesc) =>
        string.Equals(docName, apiDesc.GroupName, StringComparison.OrdinalIgnoreCase));
});


// Add Event Bus (RabbitMQ)
builder.Services.AddRabbitMQEventBus(builder.Configuration, "auth-service-queue");

// Optional Redis (to validate verification flags if needed later)
var redisEnabled = builder.Configuration.GetSection("Redis").GetValue<bool>("Enabled");
if (redisEnabled)
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration.GetSection("Redis").GetValue<string>("Configuration");
        options.InstanceName = builder.Configuration.GetSection("Redis").GetValue<string>("InstanceName");
    });
}

// Bind Frontend options for base URL resolution
builder.Services.Configure<FrontendOptions>(builder.Configuration.GetSection(FrontendOptions.SectionName));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
    app.UseSwaggerUI(c =>
    {
        provider.ApiVersionDescriptions.ToList().ForEach(description =>
            c.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", $"BookingCare Auth API {description.GroupName.ToUpperInvariant()}"));
        c.RoutePrefix = "swagger";
    });
}

app.UseGlobalExceptionHandling();

app.UseCors("AllowFrontEnd");

app.UseStandardAuthPipeline();

app.MapControllers();

// Map gRPC services
app.MapGrpcService<AuthGrpcService>();
app.MapGet("/", () => "BookingCare Auth Service is running...");

// Configure EventBus subscriptions (none for Auth now)
app.UseEventBus(eventBus => { /* No subscriptions in Auth service currently */ });

// Initialize default data
if (app.Environment.IsDevelopment())
{
    try
    {
        using var scope = app.Services.CreateScope();
        var dataInitializationService = scope.ServiceProvider.GetRequiredService<DataInitializationService>();
        await dataInitializationService.InitializeDefaultDataAsync();
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error initializing default data");
    }
}

app.Run();

