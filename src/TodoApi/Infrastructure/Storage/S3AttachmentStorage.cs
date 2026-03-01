using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using TodoApi.Application;

namespace TodoApi.Infrastructure.Storage;

public class S3AttachmentStorage : IAttachmentStorage
{
    private readonly string _bucketName;
    private readonly IAmazonS3 _client;

    public S3AttachmentStorage(IConfiguration configuration)
    {
        _bucketName = configuration["AWS:S3Bucket"]
            ?? throw new InvalidOperationException("AWS:S3Bucket is required");

        var regionName = configuration["AWS:Region"] ?? Environment.GetEnvironmentVariable("AWS_REGION") ?? "ap-northeast-1";

        var config = new AmazonS3Config
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(regionName)
        };

        var serviceUrl = configuration["AWS:S3ServiceUrl"];
        if (!string.IsNullOrWhiteSpace(serviceUrl))
        {
            config.ServiceURL = serviceUrl;
            config.ForcePathStyle = configuration.GetValue<bool?>("AWS:S3UsePathStyle") ?? true;
        }

        _client = new AmazonS3Client(config);
    }

    public async Task<string> UploadAsync(Guid todoId, string fileName, string contentType, byte[] content, CancellationToken cancellationToken = default)
    {
        var key = BuildKey(todoId, fileName);
        using var stream = new MemoryStream(content);
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = stream,
            ContentType = contentType
        };
        await _client.PutObjectAsync(request, cancellationToken);
        return key;
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        await _client.DeleteObjectAsync(_bucketName, key, cancellationToken);
    }

    public string GetReadUrl(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

        return _client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = key,
            Expires = DateTime.UtcNow.AddMinutes(15)
        });
    }

    private static string BuildKey(Guid todoId, string fileName)
    {
        var sanitized = Path.GetFileName(fileName.Trim());
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            throw new InvalidOperationException("Attachment filename is invalid");
        }

        return $"{todoId}/{sanitized}";
    }
}
