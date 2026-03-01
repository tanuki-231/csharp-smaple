namespace TodoApi.Domain.Entities;

public class TodoAttachment
{
    public Guid Id { get; set; }
    public Guid TodoRefId { get; set; }
    public string Name { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Type { get; set; } = string.Empty;
    public string S3Key { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public TodoItem Todo { get; set; } = null!;
}
