using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Wiki.UpdateWikiPageStatus;
using Domain.Wiki;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Wiki;

internal sealed class UpdateWikiPageStatus : IEndpoint
{
    public sealed record Request(WikiPageStatus Status);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("wiki/{wikiPageId:guid}/status", async (
            Guid wikiPageId,
            Request request,
            ICommandHandler<UpdateWikiPageStatusCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateWikiPageStatusCommand(wikiPageId, request.Status);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Wiki)
        .HasPermission(Permissions.Wiki.Edit);
    }
}
