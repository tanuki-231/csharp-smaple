using FluentAssertions;
using TodoApi.Domain.Exceptions;
using TodoApi.Models.Requests;
using TodoApi.Services;
using Xunit;

namespace TodoApi.Tests;

public class TodoServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldRejectInvalidStatus()
    {
        var service = new TodoService(new StubUserRepository(), new StubTodoRepository());

        var act = async () => await service.CreateAsync("demo", new TodoUpsertRequest
        {
            Title = "task",
            Description = "desc",
            Status = "unknown"
        });

        await act.Should().ThrowAsync<ValidationException>();
    }
}

internal sealed class StubUserRepository : TodoApi.Application.IUserRepository
{
    public Task AddAsync(TodoApi.Domain.Entities.User user, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<TodoApi.Domain.Entities.User?> FindByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        => Task.FromResult<TodoApi.Domain.Entities.User?>(new TodoApi.Domain.Entities.User { Id = Guid.NewGuid(), UserId = userId, PasswordHash = "hash" });
}

internal sealed class StubTodoRepository : TodoApi.Application.ITodoRepository
{
    public Task AddAsync(TodoApi.Domain.Entities.TodoItem item, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<TodoApi.Domain.Entities.TodoItem?> FindByIdAsync(Guid id, Guid userRefId, CancellationToken cancellationToken = default)
        => Task.FromResult<TodoApi.Domain.Entities.TodoItem?>(null);

    public Task<IReadOnlyList<TodoApi.Domain.Entities.TodoItem>> ListByUserAsync(Guid userRefId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<TodoApi.Domain.Entities.TodoItem>>([]);

    public Task RemoveAsync(TodoApi.Domain.Entities.TodoItem item, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
