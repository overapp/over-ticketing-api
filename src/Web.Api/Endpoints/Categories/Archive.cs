using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.TicketCategories.Archive;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Categories;

internal sealed class Archive : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("categories/{categoryId:guid}/archive", async (
            Guid categoryId,
            ICommandHandler<ArchiveTicketCategoryCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new ArchiveTicketCategoryCommand(categoryId);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Categories)
        .HasPermission(Permissions.Categories.Manage);
    }
}
