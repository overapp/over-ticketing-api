using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Tickets.UpdateStatus;
using Domain.Tickets;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Tickets;

internal sealed class UpdateStatus : IEndpoint
{
    public sealed record Request(TicketStatus Status);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("tickets/{ticketId:guid}/status", async (
            Guid ticketId,
            Request request,
            ICommandHandler<UpdateTicketStatusCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateTicketStatusCommand(ticketId, request.Status);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Tickets)
        .HasPermission(Permissions.Tickets.StatusUpdate);
    }
}
