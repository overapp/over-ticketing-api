using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Projects;
using Domain.Tickets;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Tickets.UpdateStatus;

internal sealed class UpdateTicketStatusCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<UpdateTicketStatusCommand>
{
    public async Task<Result> Handle(UpdateTicketStatusCommand command, CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        Ticket? ticket = await context.Tickets
            .SingleOrDefaultAsync(t => t.Id == command.TicketId, cancellationToken);

        if (ticket is null)
        {
            return Result.Failure(TicketErrors.NotFound(command.TicketId));
        }

        if (ticket.Status == TicketStatus.Closed)
        {
            return Result.Failure(TicketErrors.Closed(command.TicketId));
        }

        ProjectAssignment? assignment = await context.ProjectAssignments
            .AsNoTracking()
            .SingleOrDefaultAsync(pa => pa.ProjectId == ticket.ProjectId && pa.UserId == userId, cancellationToken);

        if (assignment is null)
        {
            return Result.Failure(TicketErrors.UserNotInProject(userId, ticket.ProjectId));
        }

        bool isSupportOrAdmin = string.Equals(assignment.Role, RoleNames.Support, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(assignment.Role, RoleNames.Admin, StringComparison.OrdinalIgnoreCase);

        // Standard user can only mark their own ticket as Resolved or Closed
        if (!isSupportOrAdmin)
        {
            if (ticket.CreatedByUserId != userId)
            {
                return Result.Failure(TicketErrors.UnauthorizedAccess(command.TicketId));
            }

            if (command.Status != TicketStatus.Resolved && command.Status != TicketStatus.Closed)
            {
                return Result.Failure(TicketErrors.InvalidStatusTransition(ticket.Status, command.Status));
            }
        }

        TicketStatus previousStatus = ticket.Status;
        if (previousStatus == command.Status)
        {
            return Result.Success();
        }

        DateTime now = dateTimeProvider.UtcNow;
        ticket.Status = command.Status;
        ticket.UpdatedAt = now;

        if (command.Status == TicketStatus.Resolved)
        {
            ticket.ResolvedAt = now;
        }
        else if (command.Status == TicketStatus.Closed)
        {
            ticket.ClosedAt = now;
            ticket.ResolvedAt ??= now;
        }

        ticket.Raise(new TicketStatusChangedDomainEvent(ticket.Id, previousStatus, ticket.Status));

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
