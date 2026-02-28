using Microsoft.EntityFrameworkCore;
using TodoApi.Application;
using TodoApi.Domain.Entities;
using TodoApi.Infrastructure.Data;

namespace TodoApi.Infrastructure.Repositories;

public class UserRepository(AppDbContext dbContext) : IUserRepository
{
    public Task<User?> FindByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        => dbContext.Users.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
