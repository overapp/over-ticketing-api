using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Common;
using Application.Tickets.Get;
using Domain.Tickets;
using Microsoft.AspNetCore.Http;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Tickets;

internal sealed class Get : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("projects/{projectId:guid}/tickets", async (
            Guid projectId,
            int page,
            int pageSize,
            string? searchTerm,
            TicketStatus? status,
            TicketPriority? priority,
            Guid? assignedToUserId,
            Guid? categoryId,
            IQueryHandler<GetTicketsQuery, PagedResponse<TicketSummaryResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetTicketsQuery(
                projectId,
                page == 0 ? 1 : page,
                pageSize == 0 ? 10 : pageSize,
                searchTerm,
                status,
                priority,
                assignedToUserId,
                categoryId);

            Result<PagedResponse<TicketSummaryResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Tickets)
        .WithName("GetTickets")
        .WithSummary("List a project's tickets, with search, status, priority and assignee filters.")
        .HasPermission(Permissions.Tickets.Read)
        .Produces<PagedResponse<TicketSummaryResponse>>(StatusCodes.Status200OK)
        .ProducesError(StatusCodes.Status400BadRequest, "Tickets.UserNotInProject", "The caller is not assigned to the specified project.");
    }
}
