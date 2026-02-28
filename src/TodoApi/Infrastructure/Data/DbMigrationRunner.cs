using Npgsql;

namespace TodoApi.Infrastructure.Data;

public class DbMigrationRunner(IConfiguration configuration, ILogger<DbMigrationRunner> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = configuration.GetConnectionString("PostgreSQL")
            ?? throw new InvalidOperationException("ConnectionStrings:PostgreSQL is required");

        var scriptPath = Path.Combine(AppContext.BaseDirectory, "migrations", "001_init.sql");
        if (!File.Exists(scriptPath))
        {
            logger.LogWarning("Migration script not found at {ScriptPath}", scriptPath);
            return;
        }

        var sql = await File.ReadAllTextAsync(scriptPath, cancellationToken);

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        logger.LogInformation("Database migration script applied");
    }
}
