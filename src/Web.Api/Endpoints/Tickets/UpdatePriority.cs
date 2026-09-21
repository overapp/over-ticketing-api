using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Tickets.UpdatePriority;
using Domain.Tickets;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Tickets;

internal sealed class UpdatePriority : IEndpoint
{
    public sealed record Request(TicketPriority Priority);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("tickets/{ticketId:guid}/priority", async (
            Guid ticketId,
            Request request,
            ICommandHandler<UpdateTicketPriorityCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateTicketPriorityCommand(ticketId, request.Priority);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Tickets)
        .HasPermission(Permissions.Tickets.PriorityUpdate);
    }
}
