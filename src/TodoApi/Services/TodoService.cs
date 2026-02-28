using TodoApi.Application;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Enums;
using TodoApi.Domain.Exceptions;
using TodoApi.Models.Requests;
using TodoApi.Models.Responses;

namespace TodoApi.Services;

public class TodoService(
    IUserRepository userRepository,
    ITodoRepository todoRepository) : ITodoService
{
    public async Task<IReadOnlyList<TodoResponse>> ListAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(userId, cancellationToken);
        var items = await todoRepository.ListByUserAsync(user.Id, cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<TodoResponse> CreateAsync(string userId, TodoUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(userId, cancellationToken);

        var item = new TodoItem
        {
            Id = Guid.NewGuid(),
            UserRefId = user.Id,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            Status = ParseStatus(request.Status),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await todoRepository.AddAsync(item, cancellationToken);
        await todoRepository.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task<TodoResponse> UpdateAsync(string userId, Guid id, TodoUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(userId, cancellationToken);
        var item = await todoRepository.FindByIdAsync(id, user.Id, cancellationToken) ?? throw new NotFoundException("Todo not found");

        item.Title = request.Title.Trim();
        item.Description = request.Description?.Trim() ?? string.Empty;
        item.Status = ParseStatus(request.Status);
        item.UpdatedAt = DateTimeOffset.UtcNow;

        await todoRepository.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task DeleteAsync(string userId, Guid id, CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(userId, cancellationToken);
        var item = await todoRepository.FindByIdAsync(id, user.Id, cancellationToken) ?? throw new NotFoundException("Todo not found");
        await todoRepository.RemoveAsync(item, cancellationToken);
        await todoRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<User> GetUserAsync(string userId, CancellationToken cancellationToken)
        => await userRepository.FindByUserIdAsync(userId, cancellationToken) ?? throw new ForbiddenException();

    private static TodoResponse Map(TodoItem item) => new()
    {
        Id = item.Id.ToString(),
        Title = item.Title,
        Description = item.Description,
        Status = item.Status switch
        {
            TodoStatus.Pending => "pending",
            TodoStatus.InProgress => "in_progress",
            TodoStatus.Done => "done",
            _ => throw new ValidationException("Invalid status")
        }
    };

    private static TodoStatus ParseStatus(string status) => status switch
    {
        "pending" => TodoStatus.Pending,
        "in_progress" => TodoStatus.InProgress,
        "done" => TodoStatus.Done,
        _ => throw new ValidationException("Invalid status")
    };
}
