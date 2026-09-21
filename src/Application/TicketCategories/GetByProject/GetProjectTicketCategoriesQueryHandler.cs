using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Projects;
using Domain.Tickets;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.TicketCategories.GetByProject;

internal sealed class GetProjectTicketCategoriesQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetProjectTicketCategoriesQuery, IReadOnlyCollection<TicketCategoryResponse>>
{
    public async Task<Result<IReadOnlyCollection<TicketCategoryResponse>>> Handle(
        GetProjectTicketCategoriesQuery query,
        CancellationToken cancellationToken)
    {
        bool projectExists = await context.Projects
            .AsNoTracking()
            .AnyAsync(p => p.Id == query.ProjectId, cancellationToken);

        if (!projectExists)
        {
            return Result.Failure<IReadOnlyCollection<TicketCategoryResponse>>(
                ProjectErrors.NotFound(query.ProjectId));
        }

        List<TicketCategoryResponse> categories = await context.TicketCategories
            .AsNoTracking()
            .Where(tc => tc.Status == TicketCategoryStatus.Active &&
                         (tc.ProjectId == null || tc.ProjectId == query.ProjectId))
            .OrderBy(tc => tc.ProjectId == null ? 0 : 1)
            .ThenBy(tc => tc.Name)
            .Select(tc => new TicketCategoryResponse(
                tc.Id,
                tc.ProjectId,
                tc.Name,
                tc.Description,
                tc.BackgroundColor,
                tc.ForegroundColor,
                tc.Status))
            .ToListAsync(cancellationToken);

        return categories;
    }
}
