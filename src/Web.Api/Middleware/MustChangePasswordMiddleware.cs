using System.Security.Claims;
using Application.Abstractions.Authentication;
using Domain.Users;
using Microsoft.AspNetCore.Mvc;

namespace Web.Api.Middleware;

public sealed class MustChangePasswordMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> AllowedPathPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "/auth/change-password",
        "/auth/me",
        "/auth/refresh-token",
        "/health",
        "/openapi",
        "/scalar"
    };

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity is { IsAuthenticated: true })
        {
            Claim? mustChangePasswordClaim = context.User.FindFirst(CustomClaims.MustChangePassword);

            if (mustChangePasswordClaim is not null &&
                string.Equals(mustChangePasswordClaim.Value, "true", StringComparison.OrdinalIgnoreCase))
            {
                string path = context.Request.Path.Value ?? string.Empty;

                bool isAllowed = AllowedPathPrefixes.Any(allowed =>
                    path.StartsWith(allowed, StringComparison.OrdinalIgnoreCase));

                if (!isAllowed)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/problem+json";

                    var problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status403Forbidden,
                        Type = UserErrors.MustChangePasswordRequired.Code,
                        Title = "Forbidden",
                        Detail = UserErrors.MustChangePasswordRequired.Description
                    };

                    await context.Response.WriteAsJsonAsync(problemDetails);
                    return;
                }
            }
        }

        await next(context);
    }
}
