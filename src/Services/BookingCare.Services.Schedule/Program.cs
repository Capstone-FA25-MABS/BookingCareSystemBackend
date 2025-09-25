using BookingCare.Services.Schedule.Data;
using BookingCare.Services.Schedule.Repositories;
using BookingCare.Services.Schedule.Services;
using BookingCare.Shared.Cache.Extensions;
using BookingCare.Shared.Common.Extensions;
using BookingCare.Shared.Common.Versioning;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;

// Enable HTTP/2 without TLS for gRPC (development only)
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel for both HTTP and gRPC
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(6015, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });
    options.ListenAnyIP(6025, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

// Add DbContext
builder.Services.AddDbContext<ScheduleDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Redis Cache
builder.Services.AddRedisCache(builder.Configuration);

// Add gRPC
builder.Services.AddGrpc();

// Add Controllers and API versioning
builder.Services.AddControllers();
builder.Services.AddApiVersioningSupport();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register repositories and services
builder.Services.AddScoped<IScheduleRepository, ScheduleRepository>();
builder.Services.AddScoped<IScheduleService, ScheduleService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Apply database migrations
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ScheduleDbContext>();
    context.Database.Migrate();
}

app.UseRouting();
// app.UseApiVersioning();
app.MapControllers();

// Configure gRPC services
app.MapGrpcService<ScheduleGrpcService>();
app.MapGet("/", () => "BookingCare Schedule Service is running...");

app.Run();
