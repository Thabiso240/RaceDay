using System.Security.Claims;
using RaceDay.API.Services;

namespace RaceDay.API.Security;

public class SessionAuthMiddleware
{
    private readonly RequestDelegate _next;

    public SessionAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ISessionService sessionService)
    {
        string? token = null;

        // 1. Check custom X-Session-Token header
        if (context.Request.Headers.TryGetValue("X-Session-Token", out var headerToken) && !string.IsNullOrWhiteSpace(headerToken))
        {
            token = headerToken.ToString();
        }
        // 2. Check Authorization Bearer header
        else if (context.Request.Headers.TryGetValue("Authorization", out var authHeader) && !string.IsNullOrWhiteSpace(authHeader))
        {
            var authHeaderStr = authHeader.ToString();
            if (authHeaderStr.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = authHeaderStr.Substring("Bearer ".Length).Trim();
            }
        }
        // 3. Check Cookie
        else if (context.Request.Cookies.TryGetValue("SessionToken", out var cookieToken) && !string.IsNullOrWhiteSpace(cookieToken))
        {
            token = cookieToken;
        }

        if (!string.IsNullOrWhiteSpace(token))
        {
            var session = await sessionService.ValidateSessionAsync(token);
            if (session != null && session.User != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, session.User.UserId.ToString()),
                    new Claim(ClaimTypes.Name, session.User.FullName),
                    new Claim(ClaimTypes.Email, session.User.Email),
                    new Claim(ClaimTypes.Role, session.Role),
                    new Claim("SessionToken", session.SessionToken)
                };

                var identity = new ClaimsIdentity(claims, "SessionAuth");
                context.User = new ClaimsPrincipal(identity);
            }
        }

        await _next(context);
    }
}
