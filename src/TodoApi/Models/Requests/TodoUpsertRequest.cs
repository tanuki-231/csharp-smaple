using System.ComponentModel.DataAnnotations;

namespace TodoApi.Models.Requests;

public class TodoUpsertRequest
{
    public string? Id { get; set; }

    public List<TodoAttachmentRequest>? Attachments { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required, RegularExpression("^(pending|in_progress|done)$")]
    public string Status { get; set; } = "pending";
}

public class TodoAttachmentRequest
{
    public string? Id { get; set; }

    [Required, MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public long Size { get; set; }

    [Required, MaxLength(100)]
    public string Type { get; set; } = string.Empty;

    [Required]
    public string DataUrl { get; set; } = string.Empty;
}
