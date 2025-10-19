using BookingCare.Services.Content.Services;
using BookingCare.Shared.Common.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel with security best practices
builder.WebHost.ConfigureSecureKestrel(builder.Configuration, builder.Environment, "content");

builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.MapControllers();

// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();

app.Run();
