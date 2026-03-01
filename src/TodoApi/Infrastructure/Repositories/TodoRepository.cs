using Microsoft.EntityFrameworkCore;
using TodoApi.Application;
using TodoApi.Domain.Entities;
using TodoApi.Infrastructure.Data;

namespace TodoApi.Infrastructure.Repositories;

public class TodoRepository(AppDbContext dbContext) : ITodoRepository
{
    public async Task<IReadOnlyList<TodoItem>> ListByUserAsync(Guid userRefId, CancellationToken cancellationToken = default)
        => await dbContext.Todos
            .Where(x => x.UserRefId == userRefId)
            .Include(x => x.Attachments)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<TodoItem?> FindByIdAsync(Guid id, Guid userRefId, CancellationToken cancellationToken = default)
        => dbContext.Todos
            .Include(x => x.Attachments)
            .FirstOrDefaultAsync(x => x.Id == id && x.UserRefId == userRefId, cancellationToken);

    public Task AddAsync(TodoItem item, CancellationToken cancellationToken = default)
        => dbContext.Todos.AddAsync(item, cancellationToken).AsTask();

    public Task RemoveAsync(TodoItem item, CancellationToken cancellationToken = default)
    {
        dbContext.Todos.Remove(item);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
