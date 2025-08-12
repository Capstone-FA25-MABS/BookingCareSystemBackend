using Consul;
using BookingCare.ServiceDiscovery.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Consul
builder.Services.AddSingleton<IConsulClient>(provider =>
{
    var consulConfig = new ConsulClientConfiguration
    {
        Address = new Uri(builder.Configuration["Consul:Host"]!)
    };
    return new ConsulClient(consulConfig);
});

builder.Services.AddScoped<IServiceRegistrationService, ServiceRegistrationService>();
builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
