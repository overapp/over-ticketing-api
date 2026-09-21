using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Tickets;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.TicketCategories.GetGlobal;

internal sealed class GetGlobalTicketCategoriesQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetGlobalTicketCategoriesQuery, IReadOnlyCollection<TicketCategoryResponse>>
{
    public async Task<Result<IReadOnlyCollection<TicketCategoryResponse>>> Handle(
        GetGlobalTicketCategoriesQuery query,
        CancellationToken cancellationToken)
    {
        IQueryable<TicketCategory> categoriesQuery = context.TicketCategories
            .AsNoTracking()
            .Where(tc => tc.ProjectId == null);

        if (!query.IncludeArchived)
        {
            categoriesQuery = categoriesQuery.Where(tc => tc.Status == TicketCategoryStatus.Active);
        }

        List<TicketCategoryResponse> categories = await categoriesQuery
            .OrderBy(tc => tc.Name)
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
