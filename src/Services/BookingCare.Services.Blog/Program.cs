using BookingCare.Services.Blog.Data;
using BookingCare.Services.Blog.Mappings;
using BookingCare.Services.Blog.Repositories;
using BookingCare.Services.Blog.Services;
using BookingCare.Services.Blog.Validators;
using BookingCare.Services.User.Protos;
using BookingCare.Shared.Common.Extensions;
using BookingCare.Shared.Common.Versioning;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BookingCare.Shared.FileUpload.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureSecureKestrel(builder.Configuration, builder.Environment, "blog");

builder.Services.AddDbContext<BlogDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCommonControllers();
builder.Services.AddApiVersioningSupport();
builder.Services.AddJwtAuthAndAuthorization(builder.Configuration, builder.Environment);
builder.Services.AddCommonSwagger("Blog");
builder.Services.AddGlobalExceptionHandling();
builder.Logging.AddCommonLogging();

builder.Services.AddAutoMapper(typeof(BlogMappingProfile));

builder.Services.AddValidatorsFromAssemblyContaining<CreateBlogRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();

builder.Services.AddScoped<IBlogCategoryRepository, BlogCategoryRepository>();
builder.Services.AddScoped<IBlogRepository, BlogRepository>();
builder.Services.AddScoped<IBlogCategoryService, BlogCategoryService>();
builder.Services.AddScoped<IBlogService, BlogService>();
builder.Services.AddS3FileUpload(builder.Configuration);

// Add gRPC support
builder.Services.AddGrpc();

// Add gRPC client for User service
builder.Services.AddGrpcClient<UserService.UserServiceClient>(o =>
{
    var endpoint = builder.Configuration.GetSection("Services:User").GetValue<string>("GrpcUrl") ?? "http://localhost:6116";
    o.Address = new Uri(endpoint);
});

var app = builder.Build();

app.UseCommonSwaggerUI("Blog");

// Initialize database: Apply migrations or ensure created, then seed data
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // Try to apply migrations first (preferred method)
        try
        {
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully");
        }
        catch (InvalidOperationException)
        {
            // No migrations found, use EnsureCreated (only creates if doesn't exist, doesn't delete)
            var canConnect = await dbContext.Database.CanConnectAsync();
            if (!canConnect)
            {
                await dbContext.Database.EnsureCreatedAsync();
                logger.LogInformation("Database created successfully (no migrations found, using EnsureCreated)");
            }
            else
            {
                logger.LogInformation("Database already exists (no migrations found)");
            }
        }

        // Seed initial data (only if database is empty)
        await BlogDataSeeder.SeedAsync(dbContext);
        logger.LogInformation("Database seeding completed");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error initializing database or seeding data");
        throw;
    }
}

app.UseGlobalExceptionHandling();
app.UseStandardAuthPipeline();
app.MapControllers();
app.MapCommonHealthCheck("Blog");
app.MapGet("/", () => "BookingCare Blog Service is running...");

app.Run();

