using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using BookingCare.Services.Discount.Data;
using System.Text.RegularExpressions;

namespace BookingCare.Services.Discount.Services;

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
                throw new InvalidOperationException("Connection string not configured");
            }

            var builder = new SqlConnectionStringBuilder(connectionString);
            var databaseName = builder.InitialCatalog;

            var sqlServerAvailable = await WaitForSqlServerAsync(builder);
            if (!sqlServerAvailable)
            {
                _logger.LogError("SQL Server is not available after {MaxRetries} retries", MaxRetryAttempts);
                throw new InvalidOperationException("SQL Server is not available");
            }

            var databaseExists = await CheckDatabaseExistsAsync(builder, databaseName);

            if (!databaseExists)
            {
                _logger.LogInformation("Database {DatabaseName} does not exist. Creating from SQL script...", databaseName);
                await CreateDatabaseFromScriptAsync(builder, databaseName);
                _logger.LogInformation("Database {DatabaseName} created successfully", databaseName);
            }
            else
            {
                _logger.LogInformation("Database {DatabaseName} already exists. Checking for pending migrations...", databaseName);
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<DiscountDbContext>();
                var pendingMigrations = await context.Database.GetPendingMigrationsAsync();

                if (pendingMigrations.Any())
                {
                    _logger.LogInformation("Applying {Count} pending migrations", pendingMigrations.Count());
                    await context.Database.MigrateAsync();
                    _logger.LogInformation("Migrations applied successfully");
                }
                else
                {
                    _logger.LogInformation("No pending migrations found");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database initialization");
            throw;
        }
    }

    private async Task<bool> WaitForSqlServerAsync(SqlConnectionStringBuilder builder)
    {
        var masterConnectionString = new SqlConnectionStringBuilder(builder.ConnectionString)
        {
            InitialCatalog = "master",
            ConnectTimeout = 5
        }.ConnectionString;

        for (int i = 0; i < MaxRetryAttempts; i++)
        {
            try
            {
                await using var connection = new SqlConnection(masterConnectionString);
                await connection.OpenAsync();
                _logger.LogInformation("Successfully connected to SQL Server on attempt {Attempt}", i + 1);
                return true;
            }
            catch (SqlException ex)
            {
                _logger.LogWarning(ex, "Failed to connect to SQL Server on attempt {Attempt}. Retrying in {Delay}ms...",
                    i + 1, DelayBetweenRetriesMs);

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
        var scriptPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "data", "db_discount.sql");

        var alternativePaths = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "data", "db_discount.sql"),
            Path.Combine(Directory.GetCurrentDirectory(), "data", "db_discount.sql"),
            "/app/data/db_discount.sql",
            Path.Combine("/app", "data", "db_discount.sql"),
            Path.Combine(AppContext.BaseDirectory, "data", "db_discount.sql")
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
            throw new FileNotFoundException($"SQL script file 'db_discount.sql' not found");
        }

        _logger.LogInformation("Found SQL script at: {ScriptPath}", foundScriptPath);

        var sqlScript = await File.ReadAllTextAsync(foundScriptPath);

        var masterConnectionString = new SqlConnectionStringBuilder(builder.ConnectionString)
        {
            InitialCatalog = "master"
        }.ConnectionString;

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
                command.CommandTimeout = 300;
                await command.ExecuteNonQueryAsync();
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error executing SQL batch: {Batch}", batch.Substring(0, Math.Min(100, batch.Length)));
                throw;
            }
        }

        _logger.LogInformation("Database created and initialized successfully from SQL script");
    }

    private List<string> SplitSqlScript(string script)
    {
        script = PreprocessSqlForAzureSqlEdge(script);

        var batches = new List<string>();
        var lines = script.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var currentBatch = new List<string>();

        foreach (var line in lines)
        {
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

        if (currentBatch.Count > 0)
        {
            batches.Add(string.Join(Environment.NewLine, currentBatch));
        }

        return batches;
    }

    private string PreprocessSqlForAzureSqlEdge(string script)
    {
        _logger.LogInformation("Preprocessing SQL script for Azure SQL Edge compatibility...");

        script = Regex.Replace(script, @",\s*LEDGER\s*=\s*(ON|OFF)", "", RegexOptions.IgnoreCase);

        script = Regex.Replace(script,
            @"WITH\s+CATALOG_COLLATION\s*=\s*DATABASE_DEFAULT\s*,\s*LEDGER\s*=\s*(ON|OFF)",
            "", RegexOptions.IgnoreCase);

        script = Regex.Replace(script,
            @"FILENAME\s*=\s*N'C:\\Program Files[^']+'\s*,\s*",
            "", RegexOptions.IgnoreCase);

        script = Regex.Replace(script,
            @"CREATE DATABASE \[MABS_Discount\][^\n]*\r?\n(?:\s*CONTAINMENT[^\n]*\r?\n)?(?:\s*ON\s+PRIMARY[^\n]*\r?\n(?:\([^\)]+\)[^\n]*\r?\n)?)(?:\s*LOG ON[^\n]*\r?\n(?:\([^\)]+\)[^\n]*\r?\n)?)?(?:\s*WITH[^\n]*\r?\n)?",
            "CREATE DATABASE [MABS_Discount]\n",
            RegexOptions.IgnoreCase | RegexOptions.Multiline);

        script = Regex.Replace(script,
            @"ALTER DATABASE \[MABS_Discount\] SET COMPATIBILITY_LEVEL = 160",
            "ALTER DATABASE [MABS_Discount] SET COMPATIBILITY_LEVEL = 150",
            RegexOptions.IgnoreCase);

        return script;
    }
}
