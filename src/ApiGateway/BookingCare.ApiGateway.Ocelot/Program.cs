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

// Add JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ClockSkew = TimeSpan.Zero
        };
    });

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

app.UseCors("AllowFrontEnd");
app.UseAuthentication();
app.UseOcelot().Wait();

app.Run();
