using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Tickets;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.TicketCategories.Archive;

internal sealed class ArchiveTicketCategoryCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ArchiveTicketCategoryCommand>
{
    public async Task<Result> Handle(
        ArchiveTicketCategoryCommand command,
        CancellationToken cancellationToken)
    {
        TicketCategory? category = await context.TicketCategories
            .SingleOrDefaultAsync(tc => tc.Id == command.CategoryId, cancellationToken);

        if (category is null)
        {
            return Result.Failure(TicketCategoryErrors.NotFound(command.CategoryId));
        }

        if (category.Status == TicketCategoryStatus.Archived)
        {
            return Result.Failure(TicketCategoryErrors.AlreadyArchived(command.CategoryId));
        }

        category.Status = TicketCategoryStatus.Archived;
        category.UpdatedAt = dateTimeProvider.UtcNow;

        category.Raise(new TicketCategoryArchivedDomainEvent(category.Id));

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
