using System.Security.Cryptography;
using TodoApi.Application;
using TodoApi.Domain.Exceptions;
using TodoApi.Models.Requests;
using TodoApi.Models.Responses;

namespace TodoApi.Services;

public class AuthService(
    IUserRepository userRepository,
    IPasswordHasherService passwordHasher,
    ISessionService sessionService,
    IConfiguration configuration,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.FindByUserIdAsync(request.UserId, cancellationToken);
        if (user is null || !passwordHasher.Verify(user, request.Password))
        {
            throw new UnauthorizedException("Invalid credentials");
        }

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var ttlMinutes = configuration.GetValue<int?>("Session:TTLMinutes") ?? 20;
        await sessionService.CreateSessionAsync(token, user.UserId, TimeSpan.FromMinutes(ttlMinutes), cancellationToken);

        logger.LogInformation("User {UserId} logged in", user.UserId);

        return new LoginResponse
        {
            Token = token,
            UserId = user.UserId
        };
    }
}
