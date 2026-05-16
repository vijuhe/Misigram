using System.Security.Claims;

namespace PrivateInsta.Api.Middleware;

public class GoogleAllowlistMiddleware(RequestDelegate next, IConfiguration config)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var email = context.User.FindFirstValue(ClaimTypes.Email)
                     ?? context.User.FindFirstValue("email");

            var allowed = config.GetSection("AllowedGoogleAccounts").Get<string[]>() ?? [];

            if (!allowed.Contains(email, StringComparer.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Access denied.");
                return;
            }
        }

        await next(context);
    }
}
