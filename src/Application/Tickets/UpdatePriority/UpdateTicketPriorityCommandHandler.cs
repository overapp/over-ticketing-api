using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Projects;
using Domain.Tickets;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Tickets.UpdatePriority;

internal sealed class UpdateTicketPriorityCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<UpdateTicketPriorityCommand>
{
    public async Task<Result> Handle(UpdateTicketPriorityCommand command, CancellationToken cancellationToken)
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

        if (!isSupportOrAdmin)
        {
            return Result.Failure(TicketErrors.UnauthorizedAccess(command.TicketId));
        }

        TicketPriority previousPriority = ticket.Priority;
        if (previousPriority == command.Priority)
        {
            return Result.Success();
        }

        ticket.Priority = command.Priority;
        ticket.UpdatedAt = dateTimeProvider.UtcNow;

        // Recalculate SLA due dates based on original CreatedAt and new Priority
        (DateTime firstResponseDueAt, DateTime resolutionDueAt) =
            TicketSlaCalculator.CalculateDueDates(ticket.CreatedAt, command.Priority);

        ticket.FirstResponseDueAt = firstResponseDueAt;
        ticket.ResolutionDueAt = resolutionDueAt;

        ticket.Raise(new TicketPriorityChangedDomainEvent(ticket.Id, previousPriority, ticket.Priority));

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
