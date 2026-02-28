using TodoApi.Domain.Enums;

namespace TodoApi.Domain.Entities;

public class TodoItem
{
    public Guid Id { get; set; }
    public Guid UserRefId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TodoStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User User { get; set; } = null!;
}
