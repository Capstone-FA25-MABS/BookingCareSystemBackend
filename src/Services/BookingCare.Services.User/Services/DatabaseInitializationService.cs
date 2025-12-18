using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using BookingCare.Services.User.Data;
using System.Text.RegularExpressions;

namespace BookingCare.Services.User.Services;

public class DatabaseInitializationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseInitializationService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private const int MaxRetryAttempts = 5;
    private const int DelayBetweenRetriesMs = 3000;

    public DatabaseInitializationService(
        IConfiguration configuration,
        ILogger<DatabaseInitializationService> logger,
        IServiceProvider serviceProvider)
    {
        _configuration = configuration;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task InitializeAsync()
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("Connection string 'DefaultConnection' not found");
                return;
            }

            var builder = new SqlConnectionStringBuilder(connectionString);
            var databaseName = builder.InitialCatalog;

            _logger.LogInformation("Checking if database '{DatabaseName}' exists...", databaseName);

            // Wait for SQL Server to be available with retry logic
            var sqlServerAvailable = await WaitForSqlServerAsync(builder);
            if (!sqlServerAvailable)
            {
                _logger.LogWarning("SQL Server is not available after {MaxRetry} attempts. Service will start without database initialization.", MaxRetryAttempts);
                return;
            }

            // Check if database exists
            var databaseExists = await CheckDatabaseExistsAsync(builder, databaseName);

            if (!databaseExists)
            {
                _logger.LogWarning("Database '{DatabaseName}' does not exist. Creating from SQL script...", databaseName);
                await CreateDatabaseFromScriptAsync(builder, databaseName);
                _logger.LogInformation("Database '{DatabaseName}' created successfully from SQL script", databaseName);
            }
            else
            {
                _logger.LogInformation("Database '{DatabaseName}' already exists", databaseName);

                // Run migrations if database exists
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<UserDbContext>();

                var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    _logger.LogInformation("Applying {Count} pending migrations...", pendingMigrations.Count());
                    await context.Database.MigrateAsync();
                    _logger.LogInformation("Migrations applied successfully");
                }
                else
                {
                    _logger.LogInformation("No pending migrations");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing database. Service will continue without database initialization.");
            // Don't throw - allow service to start even if DB initialization fails
        }
    }

    private async Task<bool> WaitForSqlServerAsync(SqlConnectionStringBuilder builder)
    {
        var masterConnectionString = new SqlConnectionStringBuilder(builder.ConnectionString)
        {
            InitialCatalog = "master",
            ConnectTimeout = 5 // Short timeout for each attempt
        }.ConnectionString;

        for (int i = 0; i < MaxRetryAttempts; i++)
        {
            try
            {
                _logger.LogInformation("Attempting to connect to SQL Server (attempt {Attempt}/{MaxAttempts})...", i + 1, MaxRetryAttempts);

                await using var connection = new SqlConnection(masterConnectionString);
                await connection.OpenAsync();

                _logger.LogInformation("Successfully connected to SQL Server");
                return true;
            }
            catch (SqlException ex)
            {
                _logger.LogWarning(ex, "Failed to connect to SQL Server (attempt {Attempt}/{MaxAttempts}). Retrying in {Delay}ms...",
                    i + 1, MaxRetryAttempts, DelayBetweenRetriesMs);

                if (i < MaxRetryAttempts - 1)
                {
                    await Task.Delay(DelayBetweenRetriesMs);
                }
            }
        }

        return false;
    }

    private async Task<bool> CheckDatabaseExistsAsync(SqlConnectionStringBuilder builder, string databaseName)
    {
        // Connect to master database to check if target database exists
        var masterConnectionString = new SqlConnectionStringBuilder(builder.ConnectionString)
        {
            InitialCatalog = "master"
        }.ConnectionString;

        await using var connection = new SqlConnection(masterConnectionString);
        await connection.OpenAsync();

        var query = "SELECT database_id FROM sys.databases WHERE Name = @databaseName";
        await using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@databaseName", databaseName);

        var result = await command.ExecuteScalarAsync();
        return result != null;
    }

    private async Task CreateDatabaseFromScriptAsync(SqlConnectionStringBuilder builder, string databaseName)
    {
        // Get the path to the SQL script file
        var scriptPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "data", "db_user.sql");

        // Alternative paths to check
        var alternativePaths = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "data", "db_user.sql"),
            Path.Combine(Directory.GetCurrentDirectory(), "data", "db_user.sql"),
            "/app/data/db_user.sql", // Docker path
            Path.Combine(AppContext.BaseDirectory, "data", "db_user.sql")
        };

        string? foundScriptPath = null;
        if (File.Exists(scriptPath))
        {
            foundScriptPath = scriptPath;
        }
        else
        {
            foreach (var altPath in alternativePaths)
            {
                if (File.Exists(altPath))
                {
                    foundScriptPath = altPath;
                    break;
                }
            }
        }

        if (foundScriptPath == null)
        {
            _logger.LogError("SQL script file not found at any of the expected locations");
            _logger.LogError("Tried paths: {ScriptPath} and alternatives", scriptPath);
            throw new FileNotFoundException($"SQL script file 'db_user.sql' not found");
        }

        _logger.LogInformation("Found SQL script at: {ScriptPath}", foundScriptPath);

        // Read the SQL script
        var sqlScript = await File.ReadAllTextAsync(foundScriptPath);

        // Connect to master database to create the new database
        var masterConnectionString = new SqlConnectionStringBuilder(builder.ConnectionString)
        {
            InitialCatalog = "master"
        }.ConnectionString;

        // Split the script into batches (separated by GO statements)
        var batches = SplitSqlScript(sqlScript);

        await using var connection = new SqlConnection(masterConnectionString);
        await connection.OpenAsync();

        foreach (var batch in batches)
        {
            if (string.IsNullOrWhiteSpace(batch))
                continue;

            try
            {
                await using var command = new SqlCommand(batch, connection);
                command.CommandTimeout = 300; // 5 minutes timeout
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing SQL batch: {Batch}", batch.Substring(0, Math.Min(100, batch.Length)));
                throw;
            }
        }

        _logger.LogInformation("Database created and initialized successfully from SQL script");
    }

    private List<string> SplitSqlScript(string script)
    {
        // Preprocess script to remove Azure SQL Edge incompatible syntax
        script = PreprocessSqlForAzureSqlEdge(script);

        // Split by GO statements (case insensitive)
        var batches = new List<string>();
        var lines = script.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var currentBatch = new List<string>();

        foreach (var line in lines)
        {
            // Check if line is a GO statement (with optional whitespace)
            if (line.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase))
            {
                if (currentBatch.Count > 0)
                {
                    batches.Add(string.Join(Environment.NewLine, currentBatch));
                    currentBatch.Clear();
                }
            }
            else
            {
                currentBatch.Add(line);
            }
        }

        // Add the last batch if any
        if (currentBatch.Count > 0)
        {
            batches.Add(string.Join(Environment.NewLine, currentBatch));
        }

        return batches;
    }

    private string PreprocessSqlForAzureSqlEdge(string script)
    {
        _logger.LogInformation("Preprocessing SQL script for Azure SQL Edge compatibility...");

        // Remove LEDGER options (not supported in Azure SQL Edge)
        script = Regex.Replace(script, @",\s*LEDGER\s*=\s*(ON|OFF)", "", RegexOptions.IgnoreCase);

        // Remove CATALOG_COLLATION with LEDGER
        script = Regex.Replace(script,
            @"WITH\s+CATALOG_COLLATION\s*=\s*DATABASE_DEFAULT\s*,\s*LEDGER\s*=\s*(ON|OFF)",
            "", RegexOptions.IgnoreCase);

        // Remove specific Windows file paths (let SQL Server use defaults)
        script = Regex.Replace(script,
            @"FILENAME\s*=\s*N'C:\\Program Files[^']+'\s*,\s*",
            "", RegexOptions.IgnoreCase);

        // Simplify CREATE DATABASE to basic syntax for Azure SQL Edge
        script = Regex.Replace(script,
            @"CREATE DATABASE \[MABS_User\][^\n]*\r?\n(?:\s*CONTAINMENT[^\n]*\r?\n)?(?:\s*ON\s+PRIMARY[^\n]*\r?\n(?:\([^\)]+\)[^\n]*\r?\n)?)(?:\s*LOG ON[^\n]*\r?\n(?:\([^\)]+\)[^\n]*\r?\n)?)?(?:\s*WITH[^\n]*\r?\n)?",
            "CREATE DATABASE [MABS_User]\n",
            RegexOptions.IgnoreCase | RegexOptions.Multiline);

        // Lower COMPATIBILITY_LEVEL for Azure SQL Edge (supports up to 150)
        script = Regex.Replace(script,
            @"ALTER DATABASE \[MABS_User\] SET COMPATIBILITY_LEVEL = 160",
            "ALTER DATABASE [MABS_User] SET COMPATIBILITY_LEVEL = 150",
            RegexOptions.IgnoreCase);

        // Remove FULLTEXT related commands (may not be available)
        script = Regex.Replace(script,
            @"IF \(1 = FULLTEXTSERVICEPROPERTY\('IsFullTextInstalled'\)\)[\s\S]*?end",
            "/* FULLTEXT features disabled for Azure SQL Edge */",
            RegexOptions.IgnoreCase);

        _logger.LogInformation("SQL script preprocessed successfully");
        return script;
    }
}
