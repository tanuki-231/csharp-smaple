namespace TodoApi.Models.Responses;

public class TodoResponse
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public List<TodoAttachmentResponse> Attachments { get; set; } = [];
}

public class TodoListResponse
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public int AttachmentCount { get; set; }
}

public class TodoAttachmentResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Type { get; set; } = string.Empty;
    public string DataUrl { get; set; } = string.Empty;
}
