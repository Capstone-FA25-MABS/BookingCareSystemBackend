using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text;

namespace BookingCare.Shared.Saga.Services;

/// <summary>
/// Service to initialize the Saga database schema from embedded SQL script
/// </summary>
public class SagaDatabaseInitializer
{
    private readonly string _connectionString;
    private readonly ILogger<SagaDatabaseInitializer> _logger;
    private const string SchemaResourceName = "BookingCare.Shared.Saga.Database.saga_schema.sql";

    public SagaDatabaseInitializer(string connectionString, ILogger<SagaDatabaseInitializer> logger)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initialize the database schema if it doesn't exist
    /// </summary>
    public async Task InitializeDatabaseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting Saga database initialization...");

            // First, ensure database exists
            await EnsureDatabaseExistsAsync(cancellationToken);

            // Check if tables already exist
            if (await IsDatabaseInitializedAsync(cancellationToken))
            {
                _logger.LogInformation("Saga database tables already initialized. Skipping schema initialization.");
                return;
            }

            _logger.LogInformation("Saga database exists but tables not found. Creating schema...");

            // Read embedded SQL script
            var sqlScript = ReadEmbeddedSqlScript();

            if (string.IsNullOrWhiteSpace(sqlScript))
            {
                throw new InvalidOperationException("SQL schema script is empty or not found.");
            }

            // Execute SQL script
            await ExecuteSqlScriptAsync(sqlScript, cancellationToken);

            _logger.LogInformation("Saga database initialization completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing Saga database");
            throw new InvalidOperationException("Failed to initialize Saga database. See inner exception for details.", ex);
        }
    }

    /// <summary>
    /// Ensure the database exists (creates if not exists)
    /// </summary>
    private async Task EnsureDatabaseExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Parse connection string to get database name
            var builder = new SqlConnectionStringBuilder(_connectionString);
            var databaseName = builder.InitialCatalog;

            if (string.IsNullOrWhiteSpace(databaseName))
            {
                throw new InvalidOperationException("Database name not found in connection string.");
            }

            _logger.LogInformation("Checking if database '{DatabaseName}' exists...", databaseName);

            // Create connection to master database to check/create database
            builder.InitialCatalog = "master";
            var masterConnectionString = builder.ConnectionString;

            using var connection = new SqlConnection(masterConnectionString);
            await connection.OpenAsync(cancellationToken);

            // Check if database exists
            var checkDbQuery = $@"
				SELECT COUNT(*) 
				FROM sys.databases 
				WHERE name = @DatabaseName";

            using (var checkCommand = new SqlCommand(checkDbQuery, connection))
            {
                checkCommand.Parameters.AddWithValue("@DatabaseName", databaseName);
                var count = (int)await checkCommand.ExecuteScalarAsync(cancellationToken);

                if (count > 0)
                {
                    _logger.LogInformation("Database '{DatabaseName}' already exists.", databaseName);
                    return;
                }
            }

            // Create database
            _logger.LogInformation("Creating database '{DatabaseName}'...", databaseName);

            var createDbQuery = $@"CREATE DATABASE [{databaseName}]";

            using (var createCommand = new SqlCommand(createDbQuery, connection))
            {
                await createCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            _logger.LogInformation("Database '{DatabaseName}' created successfully.", databaseName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring database exists");
            throw;
        }
    }

    /// <summary>
    /// Check if the database is already initialized by checking for the SagaStates table
    /// </summary>
    private async Task<bool> IsDatabaseInitializedAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            // Check if SagaStates table exists
            const string checkTableQuery = @"
				SELECT CASE WHEN EXISTS (
					SELECT * FROM INFORMATION_SCHEMA.TABLES 
					WHERE TABLE_NAME = 'SagaStates'
				) THEN 1 ELSE 0 END";

            using var command = new SqlCommand(checkTableQuery, connection);
            var result = await command.ExecuteScalarAsync(cancellationToken);

            return Convert.ToBoolean(result);
        }
        catch (SqlException ex)
        {
            _logger.LogWarning(ex, "Could not check database initialization status. Will attempt to initialize.");
            return false;
        }
    }

    /// <summary>
    /// Read the embedded SQL script from assembly resources
    /// </summary>
    private string ReadEmbeddedSqlScript()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();

            _logger.LogDebug("Available embedded resources: {Resources}", string.Join(", ", resourceNames));

            using var stream = assembly.GetManifestResourceStream(SchemaResourceName);

            if (stream == null)
            {
                throw new InvalidOperationException(
                    $"Embedded resource '{SchemaResourceName}' not found. " +
                    $"Available resources: {string.Join(", ", resourceNames)}");
            }

            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading embedded SQL script");
            throw;
        }
    }

    /// <summary>
    /// Execute SQL script by splitting into batches (separated by GO statements)
    /// </summary>
    private async Task ExecuteSqlScriptAsync(string sqlScript, CancellationToken cancellationToken)
    {
        // Split script by GO statements (case-insensitive)
        var batches = SplitSqlScript(sqlScript);

        _logger.LogInformation("Executing {BatchCount} SQL batches...", batches.Count);

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var batchNumber = 1;
        foreach (var batch in batches)
        {
            if (string.IsNullOrWhiteSpace(batch))
            {
                continue;
            }

            // Skip database creation statements (we handle this separately)
            var trimmedBatch = batch.Trim();
            if (trimmedBatch.Contains("CREATE DATABASE", StringComparison.OrdinalIgnoreCase) ||
                trimmedBatch.Contains("USE ", StringComparison.OrdinalIgnoreCase) ||
                (trimmedBatch.StartsWith("IF NOT EXISTS", StringComparison.OrdinalIgnoreCase) &&
                 trimmedBatch.Contains("CREATE DATABASE", StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogDebug("Skipping batch {BatchNumber} (database creation/use statement)", batchNumber);
                batchNumber++;
                continue;
            }

            try
            {
                _logger.LogDebug("Executing batch {BatchNumber}...", batchNumber);

                using var command = new SqlCommand(batch, connection)
                {
                    CommandTimeout = 300 // 5 minutes timeout
                };

                await command.ExecuteNonQueryAsync(cancellationToken);

                _logger.LogDebug("Batch {BatchNumber} executed successfully", batchNumber);
                batchNumber++;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error executing SQL batch {BatchNumber}: {Batch}", batchNumber, batch);
                throw new InvalidOperationException($"Failed to execute SQL batch {batchNumber}", ex);
            }
        }
    }

    /// <summary>
    /// Split SQL script into batches by GO statements
    /// </summary>
    private List<string> SplitSqlScript(string sqlScript)
    {
        var batches = new List<string>();
        var currentBatch = new StringBuilder();

        using var reader = new StringReader(sqlScript);
        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            // Check if line is a GO statement (case-insensitive, standalone)
            var trimmedLine = line.Trim();

            if (trimmedLine.Equals("GO", StringComparison.OrdinalIgnoreCase))
            {
                // Add current batch and start a new one
                if (currentBatch.Length > 0)
                {
                    batches.Add(currentBatch.ToString());
                    currentBatch.Clear();
                }
            }
            else
            {
                currentBatch.AppendLine(line);
            }
        }

        // Add the last batch if not empty
        if (currentBatch.Length > 0)
        {
            batches.Add(currentBatch.ToString());
        }

        return batches;
    }

    /// <summary>
    /// Force re-initialize the database (drops and recreates all objects)
    /// USE WITH CAUTION: This will delete all saga data
    /// </summary>
    public async Task ForceReinitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Force re-initialization requested. This will delete all saga data!");

        // Read and execute the full script (which includes DROP statements)
        var sqlScript = ReadEmbeddedSqlScript();
        await ExecuteSqlScriptAsync(sqlScript, cancellationToken);

        _logger.LogInformation("Force re-initialization completed.");
    }
}

