using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Wiki.UpdateProjectWikiPage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Wiki;

internal sealed class UpdateProjectWikiPage : IEndpoint
{
    public sealed record Request(
        string Title,
        string? Slug,
        string Content,
        Guid? ParentPageId,
        bool IsInternalOnly);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("projects/{projectId:guid}/wiki/{wikiPageId:guid}", async (
            Guid projectId,
            Guid wikiPageId,
            Request request,
            ICommandHandler<UpdateProjectWikiPageCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateProjectWikiPageCommand(
                projectId,
                wikiPageId,
                request.Title,
                request.Slug,
                request.Content,
                request.ParentPageId,
                request.IsInternalOnly);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Wiki)
        .HasPermission(Permissions.Wiki.Edit);
    }
}
