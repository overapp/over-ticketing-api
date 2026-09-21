using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Tickets;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.TicketCategories.Unarchive;

internal sealed class UnarchiveTicketCategoryCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<UnarchiveTicketCategoryCommand>
{
    public async Task<Result> Handle(
        UnarchiveTicketCategoryCommand command,
        CancellationToken cancellationToken)
    {
        TicketCategory? category = await context.TicketCategories
            .SingleOrDefaultAsync(tc => tc.Id == command.CategoryId, cancellationToken);

        if (category is null)
        {
            return Result.Failure(TicketCategoryErrors.NotFound(command.CategoryId));
        }

        if (category.Status == TicketCategoryStatus.Active)
        {
            return Result.Failure(TicketCategoryErrors.AlreadyActive(command.CategoryId));
        }

        category.Status = TicketCategoryStatus.Active;
        category.UpdatedAt = dateTimeProvider.UtcNow;

        category.Raise(new TicketCategoryUnarchivedDomainEvent(category.Id));

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
