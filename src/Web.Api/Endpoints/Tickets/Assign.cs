using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Tickets.Assign;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Tickets;

internal sealed class Assign : IEndpoint
{
    public sealed record Request(Guid? AssignedToUserId);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("tickets/{ticketId:guid}/assign", async (
            Guid ticketId,
            Request request,
            ICommandHandler<AssignTicketCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new AssignTicketCommand(ticketId, request.AssignedToUserId);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Tickets)
        .HasPermission(Permissions.Tickets.Assign);
    }
}
