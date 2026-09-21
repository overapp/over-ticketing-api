using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Wiki.CreateGlobalWikiPage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Wiki;

internal sealed class CreateGlobalWikiPage : IEndpoint
{
    public sealed record Request(
        string Title,
        string? Slug,
        string Content,
        Guid? ParentPageId,
        bool IsInternalOnly);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("wiki/global", async (
            Request request,
            ICommandHandler<CreateGlobalWikiPageCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateGlobalWikiPageCommand(
                request.Title,
                request.Slug,
                request.Content,
                request.ParentPageId,
                request.IsInternalOnly);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                id => Results.Created($"wiki/global/{id}", id),
                CustomResults.Problem);
        })
        .WithTags(Tags.Wiki)
        .HasPermission(Permissions.Wiki.Create);
    }
}
