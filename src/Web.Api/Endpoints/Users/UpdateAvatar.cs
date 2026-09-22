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
            .DisableAntiforgery()
            .RequireAuthorization()
            .HasPermission(Permissions.Users.Edit);
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
