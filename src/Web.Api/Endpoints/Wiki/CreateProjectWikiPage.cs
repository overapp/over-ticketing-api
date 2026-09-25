using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Wiki.CreateProjectWikiPage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Wiki;

internal sealed class CreateProjectWikiPage : IEndpoint
{
    public sealed record CreateProjectWikiPageRequest(
        string Title,
        string? Slug,
        string Content,
        Guid? ParentPageId,
        bool IsInternalOnly);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("projects/{projectId:guid}/wiki", async (
            Guid projectId,
            CreateProjectWikiPageRequest request,
            ICommandHandler<CreateProjectWikiPageCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateProjectWikiPageCommand(
                projectId,
                request.Title,
                request.Slug,
                request.Content,
                request.ParentPageId,
                request.IsInternalOnly);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                id => Results.Created($"projects/{projectId}/wiki/{id}", id),
                CustomResults.Problem);
        })
        .WithTags(Tags.Wiki)
        .WithName("CreateProjectWikiPage")
        .WithSummary("Create a wiki page scoped to a project.")
        .HasPermission(Permissions.Wiki.Create)
        .Produces<Guid>(StatusCodes.Status201Created)
        .ProducesValidationError()
        .ProducesError(StatusCodes.Status404NotFound, "Projects.NotFound", "No project exists with the specified Id.")
        .ProducesError(StatusCodes.Status400BadRequest, "WikiPages.UnauthorizedAccess", "The caller does not have permission to create wiki pages in this project.")
        .ProducesError(StatusCodes.Status404NotFound, "WikiPages.ParentNotFound", "No parent wiki page exists with the specified Id.")
        .ProducesError(StatusCodes.Status400BadRequest, "WikiPages.ParentScopeMismatch", "The parent page belongs to a different project or scope.")
        .ProducesError(StatusCodes.Status409Conflict, "WikiPages.SlugAlreadyExists", "A wiki page with the resulting slug already exists in this scope.");
    }
}
