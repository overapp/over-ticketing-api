using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Projects;
using Domain.Tickets;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Tickets.Assign;

internal sealed class AssignTicketCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<AssignTicketCommand>
{
    public async Task<Result> Handle(AssignTicketCommand command, CancellationToken cancellationToken)
    {
        Guid callerUserId = userContext.UserId;

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

        // Caller must be Support or Admin in this project
        ProjectAssignment? callerAssignment = await context.ProjectAssignments
            .AsNoTracking()
            .SingleOrDefaultAsync(pa => pa.ProjectId == ticket.ProjectId && pa.UserId == callerUserId, cancellationToken);

        if (callerAssignment is null)
        {
            return Result.Failure(TicketErrors.UserNotInProject(callerUserId, ticket.ProjectId));
        }

        bool isCallerAuthorized = string.Equals(callerAssignment.Role, RoleNames.Support, StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(callerAssignment.Role, RoleNames.Admin, StringComparison.OrdinalIgnoreCase);

        if (!isCallerAuthorized)
        {
            return Result.Failure(TicketErrors.UnauthorizedAccess(command.TicketId));
        }

        // If target assignee is specified, verify they have Support role in this project
        if (command.AssignedToUserId.HasValue)
        {
            Guid targetUserId = command.AssignedToUserId.Value;
            ProjectAssignment? targetAssignment = await context.ProjectAssignments
                .AsNoTracking()
                .SingleOrDefaultAsync(pa => pa.ProjectId == ticket.ProjectId && pa.UserId == targetUserId, cancellationToken);

            if (targetAssignment is null)
            {
                return Result.Failure(TicketErrors.UserNotInProject(targetUserId, ticket.ProjectId));
            }

            bool isTargetSupport = string.Equals(targetAssignment.Role, RoleNames.Support, StringComparison.OrdinalIgnoreCase) ||
                                   string.Equals(targetAssignment.Role, RoleNames.Admin, StringComparison.OrdinalIgnoreCase);

            if (!isTargetSupport)
            {
                return Result.Failure(TicketErrors.AssigneeNotSupport(targetUserId));
            }
        }

        ticket.AssignedToUserId = command.AssignedToUserId;
        ticket.UpdatedAt = dateTimeProvider.UtcNow;

        if (ticket.Status == TicketStatus.New && command.AssignedToUserId.HasValue)
        {
            ticket.Status = TicketStatus.InProgress;
            ticket.Raise(new TicketStatusChangedDomainEvent(ticket.Id, TicketStatus.New, TicketStatus.InProgress));
        }

        ticket.Raise(new TicketAssignedDomainEvent(ticket.Id, ticket.AssignedToUserId));

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
