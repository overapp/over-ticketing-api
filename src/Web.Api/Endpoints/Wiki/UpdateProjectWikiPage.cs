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
    public sealed record UpdateProjectWikiPageRequest(
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
            UpdateProjectWikiPageRequest request,
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
        .WithName("UpdateProjectWikiPage")
        .WithSummary("Update a project wiki page's content or move it under a different parent.")
        .HasPermission(Permissions.Wiki.Edit)
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationError()
        .ProducesError(StatusCodes.Status404NotFound, "Projects.NotFound", "No project exists with the specified Id.")
        .ProducesError(StatusCodes.Status400BadRequest, "WikiPages.UnauthorizedAccess", "The caller does not have permission to edit wiki pages in this project.")
        .ProducesError(StatusCodes.Status404NotFound, "WikiPages.NotFound", "No wiki page exists with the specified Id in this project.")
        .ProducesError(StatusCodes.Status400BadRequest, "WikiPages.CircularHierarchy", "A wiki page cannot be its own ancestor or parent.")
        .ProducesError(StatusCodes.Status404NotFound, "WikiPages.ParentNotFound", "No parent wiki page exists with the specified Id.")
        .ProducesError(StatusCodes.Status400BadRequest, "WikiPages.ParentScopeMismatch", "The parent page belongs to a different project or scope.")
        .ProducesError(StatusCodes.Status409Conflict, "WikiPages.SlugAlreadyExists", "A wiki page with the resulting slug already exists in this project.");
    }
}
