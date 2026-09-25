using Application.Abstractions.Messaging;
using Application.Users.Register;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Auth;

internal sealed class Register : IEndpoint
{
    public sealed record RegisterRequest(string Email, string FirstName, string LastName, string Password);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("auth/register", async (
            RegisterRequest request,
            ICommandHandler<RegisterUserCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new RegisterUserCommand(
                request.Email,
                request.FirstName,
                request.LastName,
                request.Password);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Auth)
        .WithName("Register")
        .WithSummary("Self-register a new user account.")
        .RequireRateLimiting(RateLimitingPolicies.Authentication)
        .Produces<Guid>(StatusCodes.Status200OK)
        .ProducesValidationError()
        .ProducesError(StatusCodes.Status409Conflict, "Users.DuplicateEmail", "A user with the provided email already exists.");
    }
}
