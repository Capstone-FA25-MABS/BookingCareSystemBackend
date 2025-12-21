using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using BookingCare.Shared.Common.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel with security best practices
builder.WebHost.ConfigureSecureKestrel(builder.Configuration, builder.Environment, "gateway");

var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();

// Add configuration
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add JWT Authentication using shared extension (includes SignalR support)
builder.Services.AddJwtAuthAndAuthorization();

// Add Ocelot
builder.Services.AddOcelot();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontEnd", builder =>
    {
        builder.WithOrigins(allowedOrigins ?? [])
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseStandardAuthPipeline();
// Enable WebSocket support for SignalR
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
});

app.UseCors("AllowFrontEnd");
app.UseOcelot().Wait();

app.Run();