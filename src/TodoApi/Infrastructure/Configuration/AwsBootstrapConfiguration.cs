using System.Text.Json;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;

namespace TodoApi.Infrastructure.Configuration;

public static class AwsBootstrapConfiguration
{
    public static async Task<IDictionary<string, string?>> LoadAsync(IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Session:TTLMinutes"] = "20"
        };

        var enabled = configuration.GetValue<bool?>("AWS:Enabled") ?? false;
        if (!enabled)
        {
            return settings;
        }

        var regionName = configuration["AWS:Region"] ?? Environment.GetEnvironmentVariable("AWS_REGION") ?? "ap-northeast-1";
        var region = RegionEndpoint.GetBySystemName(regionName);

        var ssmPrefix = configuration["AWS:SsmPrefix"] ?? "/todo-api/";
        await LoadSsmParametersAsync(settings, region, ssmPrefix, cancellationToken);

        var dbSecretId = configuration["AWS:DbSecretId"];
        if (!string.IsNullOrWhiteSpace(dbSecretId))
        {
            await LoadDbSecretAsync(settings, region, dbSecretId, cancellationToken);
        }

        return settings;
    }

    private static async Task LoadSsmParametersAsync(
        IDictionary<string, string?> settings,
        RegionEndpoint region,
        string prefix,
        CancellationToken cancellationToken)
    {
        using var client = new AmazonSimpleSystemsManagementClient(region);

        string? nextToken = null;
        do
        {
            var response = await client.GetParametersByPathAsync(new GetParametersByPathRequest
            {
                Path = prefix,
                Recursive = true,
                WithDecryption = true,
                NextToken = nextToken
            }, cancellationToken);

            foreach (var parameter in response.Parameters)
            {
                var key = parameter.Name.Replace(prefix, string.Empty).Trim('/');
                if (string.Equals(key, "RedisConnection", StringComparison.OrdinalIgnoreCase))
                {
                    settings["Redis:Connection"] = parameter.Value;
                }
                else if (string.Equals(key, "SessionTTLMinutes", StringComparison.OrdinalIgnoreCase))
                {
                    settings["Session:TTLMinutes"] = parameter.Value;
                }
                else if (string.Equals(key, "AllowedOrigins", StringComparison.OrdinalIgnoreCase))
                {
                    var origins = parameter.Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    for (var i = 0; i < origins.Length; i++)
                    {
                        settings[$"Cors:AllowedOrigins:{i}"] = origins[i];
                    }
                }
                else if (string.Equals(key, "CloudWatchLogGroup", StringComparison.OrdinalIgnoreCase))
                {
                    settings["AWS:LogGroup"] = parameter.Value;
                }
                else if (string.Equals(key, "S3Bucket", StringComparison.OrdinalIgnoreCase))
                {
                    settings["AWS:S3Bucket"] = parameter.Value;
                }
            }

            nextToken = response.NextToken;
        } while (!string.IsNullOrWhiteSpace(nextToken));
    }

    private static async Task LoadDbSecretAsync(
        IDictionary<string, string?> settings,
        RegionEndpoint region,
        string secretId,
        CancellationToken cancellationToken)
    {
        using var client = new AmazonSecretsManagerClient(region);
        var response = await client.GetSecretValueAsync(new GetSecretValueRequest
        {
            SecretId = secretId
        }, cancellationToken);

        var secretString = response.SecretString;
        if (string.IsNullOrWhiteSpace(secretString))
        {
            return;
        }

        using var doc = JsonDocument.Parse(secretString);
        if (doc.RootElement.TryGetProperty("connectionString", out var conn))
        {
            settings["ConnectionStrings:PostgreSQL"] = conn.GetString();
            return;
        }

        var host = doc.RootElement.GetProperty("host").GetString();
        var port = doc.RootElement.TryGetProperty("port", out var portValue) ? portValue.GetInt32() : 5432;
        var username = doc.RootElement.GetProperty("username").GetString();
        var password = doc.RootElement.GetProperty("password").GetString();
        var dbname = doc.RootElement.TryGetProperty("dbname", out var dbNameValue)
            ? dbNameValue.GetString()
            : doc.RootElement.GetProperty("database").GetString();

        settings["ConnectionStrings:PostgreSQL"] =
            $"Host={host};Port={port};Database={dbname};Username={username};Password={password}";
    }
}
