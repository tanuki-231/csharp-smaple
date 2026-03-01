using TodoApi.Domain.Entities;
using TodoApi.Models.Requests;
using TodoApi.Models.Responses;

namespace TodoApi.Application;

public interface IUserRepository
{
    Task<User?> FindByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
}

public interface ITodoRepository
{
    Task<IReadOnlyList<TodoItem>> ListByUserAsync(Guid userRefId, CancellationToken cancellationToken = default);
    Task<TodoItem?> FindByIdAsync(Guid id, Guid userRefId, CancellationToken cancellationToken = default);
    Task AddAsync(TodoItem item, CancellationToken cancellationToken = default);
    Task RemoveAsync(TodoItem item, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IPasswordHasherService
{
    string HashPassword(User user, string rawPassword);
    bool Verify(User user, string rawPassword);
}

public interface ISessionService
{
    Task CreateSessionAsync(string token, string userId, TimeSpan ttl, CancellationToken cancellationToken = default);
    Task<string?> GetUserIdByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task RemoveSessionAsync(string token, CancellationToken cancellationToken = default);
}

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}

public interface IAttachmentStorage
{
    Task<string> UploadAsync(Guid todoId, string fileName, string contentType, byte[] content, CancellationToken cancellationToken = default);
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
    string GetReadUrl(string key);
}

public interface ITodoService
{
    Task<IReadOnlyList<TodoListResponse>> ListAsync(string userId, CancellationToken cancellationToken = default);
    Task<TodoResponse> CreateAsync(string userId, TodoUpsertRequest request, CancellationToken cancellationToken = default);
    Task<TodoResponse> UpdateAsync(string userId, Guid id, TodoUpsertRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(string userId, Guid id, CancellationToken cancellationToken = default);
}
