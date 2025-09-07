namespace BookingCare.Shared.Common.Examples;

/// <summary>
/// Example showing how to use JWT Authentication and Authorization extensions
/// Copy this code to your service's Program.cs
/// </summary>
public static class ServiceConfigurationExample
{
    /// <summary>
    /// Example: Basic JWT setup with environment detection
    /// Copy this to your Program.cs
    /// </summary>
    public static string BasicJwtSetupExample()
    {
        return """
        // Add JWT Authentication and Authorization
        builder.Services.AddJwtAuthAndAuthorization(builder.Configuration, builder.Environment);
        """;
    }

    /// <summary>
    /// Example: JWT setup with custom configuration
    /// Copy this to your Program.cs
    /// </summary>
    public static string CustomJwtSetupExample()
    {
        return """
        // Add JWT Authentication and Authorization with custom JWT options
        builder.Services.AddJwtAuthAndAuthorization(builder.Configuration, jwtOptions =>
        {
            jwtOptions.RequireHttpsMetadata = true; // For production
            jwtOptions.SaveToken = true; // Save token in AuthenticationProperties
        });
        """;
    }

    /// <summary>
    /// Example: JWT setup with standard authorization only (no dynamic policies)
    /// Copy this to your Program.cs
    /// </summary>
    public static string StandardAuthOnlyExample()
    {
        return """
        // Add JWT Authentication and standard authorization only
        builder.Services.AddJwtAuthAndAuthorization(builder.Configuration, builder.Environment, useDynamicAuthorization: false);
        """;
    }

    /// <summary>
    /// Example: Complete Program.cs setup
    /// Copy this to your Program.cs
    /// </summary>
    public static string CompleteProgramSetupExample()
    {
        return """
        // Services
        builder.Services.AddJwtAuthAndAuthorization(builder.Configuration, builder.Environment);
        
        // Add your other services here
        // builder.Services.AddScoped<YourService>();
        """;
    }

    /// <summary>
    /// Example: Complete middleware pipeline setup
    /// Copy this to your Program.cs
    /// </summary>
    public static string CompleteMiddlewareSetupExample()
    {
        return """
        // Use standard authentication pipeline
        app.UseStandardAuthPipeline();
        
        // Map your controllers
        app.MapControllers();
        """;
    }
}
