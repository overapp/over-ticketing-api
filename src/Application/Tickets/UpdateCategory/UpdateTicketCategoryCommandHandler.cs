using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Projects;
using Domain.Tickets;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Tickets.UpdateCategory;

internal sealed class UpdateTicketCategoryCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<UpdateTicketCategoryCommand>
{
    public async Task<Result> Handle(UpdateTicketCategoryCommand command, CancellationToken cancellationToken)
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

        if (command.CategoryId.HasValue)
        {
            TicketCategory? category = await context.TicketCategories
                .AsNoTracking()
                .SingleOrDefaultAsync(tc => tc.Id == command.CategoryId.Value, cancellationToken);

            if (category is null)
            {
                return Result.Failure(TicketCategoryErrors.NotFound(command.CategoryId.Value));
            }

            if (category.Status == TicketCategoryStatus.Archived)
            {
                return Result.Failure(TicketCategoryErrors.CategoryArchived(command.CategoryId.Value));
            }

            if (category.ProjectId.HasValue && category.ProjectId.Value != ticket.ProjectId)
            {
                return Result.Failure(TicketCategoryErrors.InvalidForProject(command.CategoryId.Value, ticket.ProjectId));
            }
        }

        if (ticket.CategoryId == command.CategoryId)
        {
            return Result.Success();
        }

        Guid? previousCategoryId = ticket.CategoryId;
        ticket.CategoryId = command.CategoryId;
        ticket.UpdatedAt = dateTimeProvider.UtcNow;

        ticket.Raise(new TicketCategoryChangedDomainEvent(ticket.Id, previousCategoryId, command.CategoryId));

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
