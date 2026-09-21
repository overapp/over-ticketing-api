using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Tickets.UpdateCategory;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Tickets;

internal sealed class UpdateCategory : IEndpoint
{
    public sealed record Request(Guid? CategoryId);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("tickets/{ticketId:guid}/category", async (
            Guid ticketId,
            Request request,
            ICommandHandler<UpdateTicketCategoryCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateTicketCategoryCommand(ticketId, request.CategoryId);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Tickets)
        .HasPermission(Permissions.Tickets.CategoryUpdate);
    }
}
