using BookingCare.Services.Communication.Services;
using BookingCare.Services.Communication.Extensions;
using BookingCare.Shared.Common.Extensions;
using Microsoft.AspNetCore.Server.Kestrel.Core;

// Enable HTTP/2 without TLS for gRPC (development only)
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(6005, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });
    options.ListenAnyIP(6015, listenOptions =>
    {
        // listenOptions.UseHttps();
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add MongoDB configuration
builder.Services.AddMongoDb(builder.Configuration);

// Add health checks
builder.Services.AddHealthChecks();

// Add global exception handling
builder.Services.AddGlobalExceptionHandling();

var app = builder.Build();

// Initialize MongoDB indexes
await app.Services.InitializeMongoDbAsync();

// Use global exception handling (early in pipeline)
app.UseGlobalExceptionHandling();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();

// Add health check endpoint
app.MapHealthChecks("/health");

app.MapControllers();

// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();

app.Run();
