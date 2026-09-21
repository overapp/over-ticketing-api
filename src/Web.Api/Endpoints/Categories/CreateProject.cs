using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.TicketCategories.CreateProject;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Categories;

internal sealed class CreateProject : IEndpoint
{
    public sealed record Request(
        string Name,
        string? Description,
        string? BackgroundColor,
        string? ForegroundColor);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("projects/{projectId:guid}/categories", async (
            Guid projectId,
            Request request,
            ICommandHandler<CreateProjectTicketCategoryCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateProjectTicketCategoryCommand(
                projectId,
                request.Name,
                request.Description,
                request.BackgroundColor,
                request.ForegroundColor);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                id => Results.Created($"projects/{projectId}/categories/{id}", id),
                CustomResults.Problem);
        })
        .WithTags(Tags.Categories)
        .HasPermission(Permissions.Categories.Manage);
    }
}
