using BookingCare.Services.Auth.Services;
using BookingCare.Services.Auth.Services.Interfaces;
using BookingCare.Shared.Common.Extensions;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Shared.Saga.Extensions;
using BookingCare.Shared.Saga.Examples;
using BookingCare.Shared.EventBus.Extensions;
using Microsoft.AspNetCore.Server.Kestrel.Core;

// Enable HTTP/2 without TLS for gRPC (development only)
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(6003, listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
    });
    options.ListenAnyIP(6013, listenOptions =>
    {
        // listenOptions.UseHttps();
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
    });
});

builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddEndpointsApiExplorer();

// Add API versioning support
builder.Services.AddApiVersioningSupport();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1.0", new() { Title = "BookingCare Auth API", Version = "v1.0" });
});

builder.Services.AddGlobalExceptionHandling();

// Your existing services
// builder.Services.AddDbContext<AuthDbContext>(options =>
//     options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IAuthService, AuthService>();

// Add saga orchestration
builder.Services.AddSagaOrchestration(builder.Configuration);

// Add EventBus for RabbitMQ communication
builder.Services.AddRabbitMQEventBus(builder.Configuration, "auth_service_queue");

// Register gRPC saga steps
builder.Services.AddGrpcSagaSteps();

// Register saga definitions
builder.Services.AddSaga<UserRegistrationGrpcSaga>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1.0/swagger.json", "BookingCare Auth API V1.0");
        c.SwaggerEndpoint("/swagger/v1.1/swagger.json", "BookingCare Auth API V1.1");
        c.RoutePrefix = "swagger";
    });
}

app.UseGlobalExceptionHandling();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();
app.MapGrpcService<AuthGrpcService>();
app.MapGet("/", () => "BookingCare Auth Service is running...");

await app.RunAsync();
