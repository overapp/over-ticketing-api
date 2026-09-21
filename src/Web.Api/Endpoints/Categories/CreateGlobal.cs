using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.TicketCategories.CreateGlobal;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Categories;

internal sealed class CreateGlobal : IEndpoint
{
    public sealed record Request(
        string Name,
        string? Description,
        string? BackgroundColor,
        string? ForegroundColor);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("categories", async (
            Request request,
            ICommandHandler<CreateGlobalTicketCategoryCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateGlobalTicketCategoryCommand(
                request.Name,
                request.Description,
                request.BackgroundColor,
                request.ForegroundColor);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                id => Results.Created($"categories/{id}", id),
                CustomResults.Problem);
        })
        .WithTags(Tags.Categories)
        .HasPermission(Permissions.Categories.Manage);
    }
}
