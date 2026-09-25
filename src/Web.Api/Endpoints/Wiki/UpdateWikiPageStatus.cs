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
    public sealed record UpdateWikiPageStatusRequest(WikiPageStatus Status);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("wiki/{wikiPageId:guid}/status", async (
            Guid wikiPageId,
            UpdateWikiPageStatusRequest request,
            ICommandHandler<UpdateWikiPageStatusCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateWikiPageStatusCommand(wikiPageId, request.Status);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Wiki)
        .WithName("UpdateWikiPageStatus")
        .WithSummary("Publish, unpublish or archive a wiki page.")
        .HasPermission(Permissions.Wiki.Edit)
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationError()
        .ProducesError(StatusCodes.Status404NotFound, "WikiPages.NotFound", "No wiki page exists with the specified Id.")
        .ProducesError(StatusCodes.Status400BadRequest, "WikiPages.GlobalPagesRequireAdmin", "Only system administrators can change the status of global wiki pages.")
        .ProducesError(StatusCodes.Status400BadRequest, "WikiPages.UnauthorizedAccess", "The caller does not have permission to change the status of this wiki page.")
        .ProducesError(StatusCodes.Status400BadRequest, "WikiPages.HasChildPages", "The wiki page cannot be archived because it has active child pages.");
    }
}
