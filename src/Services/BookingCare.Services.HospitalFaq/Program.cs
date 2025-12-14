using BookingCare.Services.HospitalFaq.Data;
using BookingCare.Services.HospitalFaq.Mappings;
using BookingCare.Services.HospitalFaq.Repositories;
using BookingCare.Services.HospitalFaq.Services;
using BookingCare.Services.HospitalFaq.Validators;
using BookingCare.Shared.Common.Extensions;
using BookingCare.Shared.Common.Versioning;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureSecureKestrel(builder.Configuration, builder.Environment, "hospitalfaq");

builder.Services.AddDbContext<HospitalFaqDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCommonControllers();
builder.Services.AddApiVersioningSupport();
builder.Services.AddJwtAuthAndAuthorization(builder.Configuration, builder.Environment);
// builder.Services.AddCommonSwagger("Hospital FAQ"); // Swagger disabled
builder.Services.AddGlobalExceptionHandling();
builder.Logging.AddCommonLogging();

builder.Services.AddAutoMapper(typeof(HospitalFaqMappingProfile));

builder.Services.AddValidatorsFromAssemblyContaining<CreateHospitalFaqRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();

builder.Services.AddScoped<IHospitalFaqRepository, HospitalFaqRepository>();
builder.Services.AddScoped<IHospitalFaqService, HospitalFaqService>();

var app = builder.Build();

// app.UseCommonSwaggerUI("Hospital FAQ"); // Swagger disabled

// Initialize database: Create database if not exists, then apply migrations
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<HospitalFaqDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // Check if database can connect
        var canConnect = await dbContext.Database.CanConnectAsync();
        
        if (!canConnect)
        {
            logger.LogInformation("Database does not exist. Creating database...");
            // Create database if it doesn't exist
            await dbContext.Database.EnsureCreatedAsync();
            logger.LogInformation("Database created successfully");
        }
        else
        {
            logger.LogInformation("Database connection verified");
        }

        // Try to apply migrations (if any exist)
        try
        {
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                logger.LogInformation("Applying {Count} pending migration(s)...", pendingMigrations.Count());
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Database migrations applied successfully");
            }
            else
            {
                logger.LogInformation("Database is up to date (no pending migrations)");
            }
        }
        catch (InvalidOperationException ex)
        {
            // No migrations found - this is OK if using EnsureCreated
            logger.LogInformation("No migrations found. Database schema created using EnsureCreated method.");
        }
        catch (Exception ex)
        {
            // If migrations fail but database exists, log warning but don't fail startup
            logger.LogWarning(ex, "Failed to apply migrations, but database exists. Service will continue.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error initializing database");
        throw;
    }
}

app.UseGlobalExceptionHandling();
app.UseStandardAuthPipeline();
app.MapControllers();
app.MapCommonHealthCheck("Hospital FAQ");
app.MapGet("/", () => "BookingCare Hospital FAQ Service is running...");

app.Run();

