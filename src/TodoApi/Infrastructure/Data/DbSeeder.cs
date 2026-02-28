using Microsoft.EntityFrameworkCore;
using TodoApi.Application;
using TodoApi.Domain.Entities;

namespace TodoApi.Infrastructure.Data;

public class DbSeeder(
    AppDbContext dbContext,
    IPasswordHasherService passwordHasher,
    ILogger<DbSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.Users.AnyAsync(x => x.UserId == "demo", cancellationToken);
        if (exists)
        {
            return;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserId = "demo",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        user.PasswordHash = passwordHasher.HashPassword(user, "password");

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seed user created: demo/password");
    }
}
