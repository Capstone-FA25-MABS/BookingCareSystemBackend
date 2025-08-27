using BookingCare.Services.Appointment.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;

// Enable HTTP/2 without TLS for gRPC (development only)
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(6002, listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
    });
    options.ListenAnyIP(6012, listenOptions =>
    {
        // listenOptions.UseHttps();
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
    });
});

builder.Services.AddControllers();
builder.Services.AddGrpc();

var app = builder.Build();

app.UseRouting();
app.MapControllers();

// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();
app.MapGet("/", () => "BookingCare Appointment Service is running...");

app.Run();
