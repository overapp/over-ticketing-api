using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Tickets;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.TicketCategories.CreateGlobal;

internal sealed class CreateGlobalTicketCategoryCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateGlobalTicketCategoryCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreateGlobalTicketCategoryCommand command,
        CancellationToken cancellationToken)
    {
        string trimmedName = command.Name.Trim();

        bool nameExists = await context.TicketCategories
            .AsNoTracking()
            .AnyAsync(
                tc => tc.ProjectId == null && string.Equals(tc.Name, trimmedName, StringComparison.OrdinalIgnoreCase),
                cancellationToken);

        if (nameExists)
        {
            return Result.Failure<Guid>(TicketCategoryErrors.NameNotUnique(trimmedName));
        }

        var category = new TicketCategory
        {
            Id = Guid.NewGuid(),
            ProjectId = null,
            Name = trimmedName,
            Description = command.Description?.Trim() ?? string.Empty,
            BackgroundColor = string.IsNullOrWhiteSpace(command.BackgroundColor)
                ? "#64748B"
                : command.BackgroundColor.Trim(),
            ForegroundColor = string.IsNullOrWhiteSpace(command.ForegroundColor)
                ? "#FFFFFF"
                : command.ForegroundColor.Trim(),
            Status = TicketCategoryStatus.Active,
            CreatedAt = dateTimeProvider.UtcNow
        };

        category.Raise(new TicketCategoryCreatedDomainEvent(category.Id));

        context.TicketCategories.Add(category);

        await context.SaveChangesAsync(cancellationToken);

        return category.Id;
    }
}
