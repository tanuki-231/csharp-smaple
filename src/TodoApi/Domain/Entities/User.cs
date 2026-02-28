namespace TodoApi.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<TodoItem> Todos { get; set; } = new List<TodoItem>();
}
