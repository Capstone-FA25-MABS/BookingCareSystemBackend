using BookingCare.Services.User.Data;
using BookingCare.Services.User.Mappings;
using BookingCare.Services.User.Repositories;
using BookingCare.Services.User.Services;
using BookingCare.Shared.Common.Extensions;
using BookingCare.Shared.Common.Versioning;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;

// Enable HTTP/2 without TLS for gRPC (development only)
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(6014, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });
    options.ListenAnyIP(6024, listenOptions =>
    {
        // listenOptions.UseHttps();
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

// Add DbContext
builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register AutoMapper
builder.Services.AddAutoMapper(typeof(UserMappingProfile));

// Register repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Register services
builder.Services.AddScoped<IUserService, UserService>();

// Add global exception handling
builder.Services.AddGlobalExceptionHandling();

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddEndpointsApiExplorer();

// Add API versioning support
builder.Services.AddApiVersioningSupport();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1.0", new() { Title = "BookingCare User API", Version = "v1.0" });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    await context.Database.EnsureCreatedAsync();
}
app.UseGlobalExceptionHandling();
app.UseCors("AllowAll");
app.UseRouting();
app.MapControllers();

// Configure gRPC services
app.MapGrpcService<UserGrpcService>();
app.MapGet("/", () => "BookingCare User Service is running...");

app.Run();
