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
    public sealed record CreateGlobalWikiPageRequest(
        string Title,
        string? Slug,
        string Content,
        Guid? ParentPageId,
        bool IsInternalOnly);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("wiki/global", async (
            CreateGlobalWikiPageRequest request,
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
        .WithName("CreateGlobalWikiPage")
        .WithSummary("Create a wiki page visible across all projects.")
        .HasPermission(Permissions.Wiki.Create)
        .Produces<Guid>(StatusCodes.Status201Created)
        .ProducesValidationError()
        .ProducesError(StatusCodes.Status400BadRequest, "WikiPages.GlobalPagesRequireAdmin", "Only system administrators can create global wiki pages.")
        .ProducesError(StatusCodes.Status404NotFound, "WikiPages.ParentNotFound", "No parent wiki page exists with the specified Id.")
        .ProducesError(StatusCodes.Status400BadRequest, "WikiPages.ParentScopeMismatch", "The parent page belongs to a different project or scope.")
        .ProducesError(StatusCodes.Status409Conflict, "WikiPages.SlugAlreadyExists", "A wiki page with the resulting slug already exists in this scope.");
    }
}
