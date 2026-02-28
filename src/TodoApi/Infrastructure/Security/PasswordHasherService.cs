using Microsoft.AspNetCore.Identity;
using TodoApi.Application;
using TodoApi.Domain.Entities;

namespace TodoApi.Infrastructure.Security;

public class PasswordHasherService : IPasswordHasherService
{
    private readonly PasswordHasher<User> _hasher = new();

    public string HashPassword(User user, string rawPassword)
        => _hasher.HashPassword(user, rawPassword);

    public bool Verify(User user, string rawPassword)
        => _hasher.VerifyHashedPassword(user, user.PasswordHash, rawPassword) != PasswordVerificationResult.Failed;
}
