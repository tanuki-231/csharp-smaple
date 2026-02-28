using TodoApi.Application;
using TodoApi.Domain.Exceptions;

namespace TodoApi.Middleware;

public class TokenAuthenticationMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context, ISessionService sessionService)
    {
        if (!RequiresAuthentication(context.Request.Path))
        {
            await next(context);
            return;
        }

        var header = context.Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedException("Missing bearer token");
        }

        var token = header["Bearer ".Length..].Trim();
        var userId = await sessionService.GetUserIdByTokenAsync(token);
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new SessionTimeoutException("session expired");
        }

        context.Items["UserId"] = userId;
        await next(context);
    }

    private static bool RequiresAuthentication(PathString path)
        => path.StartsWithSegments("/api/todos") || path.StartsWithSegments("/api/logout");
}
