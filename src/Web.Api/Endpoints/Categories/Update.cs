using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.TicketCategories.Update;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Categories;

internal sealed class Update : IEndpoint
{
    public sealed record Request(
        string Name,
        string? Description,
        string? BackgroundColor,
        string? ForegroundColor);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("categories/{categoryId:guid}", async (
            Guid categoryId,
            Request request,
            ICommandHandler<UpdateTicketCategoryCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateTicketCategoryCommand(
                categoryId,
                request.Name,
                request.Description,
                request.BackgroundColor,
                request.ForegroundColor);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Categories)
        .HasPermission(Permissions.Categories.Manage);
    }
}
