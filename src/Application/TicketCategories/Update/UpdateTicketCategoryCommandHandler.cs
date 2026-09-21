using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Tickets;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.TicketCategories.Update;

internal sealed class UpdateTicketCategoryCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<UpdateTicketCategoryCommand>
{
    public async Task<Result> Handle(
        UpdateTicketCategoryCommand command,
        CancellationToken cancellationToken)
    {
        TicketCategory? category = await context.TicketCategories
            .SingleOrDefaultAsync(tc => tc.Id == command.CategoryId, cancellationToken);

        if (category is null)
        {
            return Result.Failure(TicketCategoryErrors.NotFound(command.CategoryId));
        }

        string trimmedName = command.Name.Trim();

        bool nameExists = await context.TicketCategories
            .AsNoTracking()
            .AnyAsync(
                tc => tc.Id != category.Id &&
                      tc.ProjectId == category.ProjectId &&
                      string.Equals(tc.Name, trimmedName, StringComparison.OrdinalIgnoreCase),
                cancellationToken);

        if (nameExists)
        {
            return Result.Failure(TicketCategoryErrors.NameNotUnique(trimmedName));
        }

        category.Name = trimmedName;
        category.Description = command.Description?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(command.BackgroundColor))
        {
            category.BackgroundColor = command.BackgroundColor.Trim();
        }

        if (!string.IsNullOrWhiteSpace(command.ForegroundColor))
        {
            category.ForegroundColor = command.ForegroundColor.Trim();
        }

        category.UpdatedAt = dateTimeProvider.UtcNow;

        category.Raise(new TicketCategoryUpdatedDomainEvent(category.Id));

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
