using Application.Abstractions.Authentication;
using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Users.UpdateAvatar;
using Microsoft.AspNetCore.Http;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class UpdateAvatar : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/{userId:guid}/avatar", Handle)
            .WithTags(Tags.Users)
            .WithName("UpdateUserAvatar")
            .WithSummary("Upload or replace the user's profile picture.")
            .DisableAntiforgery()
            .RequireAuthorization()
            .HasPermission(Permissions.Users.Edit)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<string>(StatusCodes.Status400BadRequest, "text/plain")
            .ProducesValidationError()
            .ProducesError(StatusCodes.Status404NotFound, "Users.NotFound", "No user exists with the specified Id.")
            .ProducesError(StatusCodes.Status500InternalServerError, "Users.UpdateFailed", "Failed to update the user with the new avatar.");
    }

    private static async Task<IResult> Handle(
        Guid userId,
        HttpRequest request,
        IUserContext userContext,
        ICommandHandler<UpdateUserAvatarCommand> handler,
        CancellationToken cancellationToken)
    {
        if (userId != userContext.UserId)
        {
            return Results.Forbid();
        }

        if (!request.HasFormContentType)
        {
            return Results.BadRequest("Expected multipart/form-data content type.");
        }

        IFormCollection form = await request.ReadFormAsync(cancellationToken);

        if (form.Files.Count == 0)
        {
            return Results.BadRequest("No file provided.");
        }

        IFormFile file = form.Files[0];

        using Stream stream = file.OpenReadStream();
        var command = new UpdateUserAvatarCommand(
            userId,
            file.FileName,
            file.ContentType,
            file.Length,
            stream);

        Result result = await handler.Handle(command, cancellationToken);

        return result.Match(
            () => Results.Ok(),
            failure => CustomResults.Problem(failure));
    }
}
