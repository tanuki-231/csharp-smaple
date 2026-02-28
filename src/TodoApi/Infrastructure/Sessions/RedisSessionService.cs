using StackExchange.Redis;
using TodoApi.Application;

namespace TodoApi.Infrastructure.Sessions;

public class RedisSessionService(IConnectionMultiplexer mux) : ISessionService
{
    private const string Prefix = "session:";

    public Task CreateSessionAsync(string token, string userId, TimeSpan ttl, CancellationToken cancellationToken = default)
        => mux.GetDatabase().StringSetAsync(Prefix + token, userId, ttl);

    public async Task<string?> GetUserIdByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var value = await mux.GetDatabase().StringGetAsync(Prefix + token);
        return value.HasValue ? value.ToString() : null;
    }

    public Task RemoveSessionAsync(string token, CancellationToken cancellationToken = default)
        => mux.GetDatabase().KeyDeleteAsync(Prefix + token);
}
