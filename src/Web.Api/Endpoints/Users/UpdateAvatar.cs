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
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(
        Guid userId,
        HttpRequest request,
        ICommandHandler<UpdateUserAvatarCommand> handler,
        CancellationToken cancellationToken)
    {
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

        var command = new UpdateUserAvatarCommand(
            userId,
            file.FileName,
            file.ContentType,
            file.Length,
            file.OpenReadStream());

        Result result = await handler.Handle(command, cancellationToken);

        return result.Match(
            () => Results.Ok(),
            failure => CustomResults.Problem(failure));
    }
}
