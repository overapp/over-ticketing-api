using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Projects;
using Domain.Tickets;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.TicketCategories.CreateProject;

internal sealed class CreateProjectTicketCategoryCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateProjectTicketCategoryCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreateProjectTicketCategoryCommand command,
        CancellationToken cancellationToken)
    {
        bool projectExists = await context.Projects
            .AsNoTracking()
            .AnyAsync(p => p.Id == command.ProjectId, cancellationToken);

        if (!projectExists)
        {
            return Result.Failure<Guid>(ProjectErrors.NotFound(command.ProjectId));
        }

        string trimmedName = command.Name.Trim();

        bool nameExists = await context.TicketCategories
            .AsNoTracking()
            .AnyAsync(
                tc => tc.ProjectId == command.ProjectId && string.Equals(tc.Name, trimmedName, StringComparison.OrdinalIgnoreCase),
                cancellationToken);

        if (nameExists)
        {
            return Result.Failure<Guid>(TicketCategoryErrors.NameNotUnique(trimmedName));
        }

        var category = new TicketCategory
        {
            Id = Guid.NewGuid(),
            ProjectId = command.ProjectId,
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
