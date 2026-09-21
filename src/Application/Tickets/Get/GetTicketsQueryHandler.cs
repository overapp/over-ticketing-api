using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Application.TicketCategories;
using Domain.Projects;
using Domain.Tickets;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Tickets.Get;

internal sealed class GetTicketsQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : IQueryHandler<GetTicketsQuery, PagedResponse<TicketSummaryResponse>>
{
    public async Task<Result<PagedResponse<TicketSummaryResponse>>> Handle(
        GetTicketsQuery query,
        CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        // Check project assignment and role
        ProjectAssignment? assignment = await context.ProjectAssignments
            .AsNoTracking()
            .SingleOrDefaultAsync(pa => pa.ProjectId == query.ProjectId && pa.UserId == userId, cancellationToken);

        if (assignment is null)
        {
            return Result.Failure<PagedResponse<TicketSummaryResponse>>(
                TicketErrors.UserNotInProject(userId, query.ProjectId));
        }

        bool isSupportOrAdmin = string.Equals(assignment.Role, RoleNames.Support, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(assignment.Role, RoleNames.Admin, StringComparison.OrdinalIgnoreCase);

        int page = query.Page < 1 ? 1 : query.Page;
        int pageSize = query.PageSize < 1 ? 10 : query.PageSize;
        if (pageSize > 100)
        {
            pageSize = 100;
        }

        IQueryable<Ticket> ticketsQuery = context.Tickets
            .AsNoTracking()
            .Where(t => t.ProjectId == query.ProjectId);

        // Standard user sees only tickets they created
        if (!isSupportOrAdmin)
        {
            ticketsQuery = ticketsQuery.Where(t => t.CreatedByUserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            string term = $"%{query.SearchTerm.Trim()}%";
            ticketsQuery = ticketsQuery.Where(t => EF.Functions.Like(t.Title, term));
        }

        if (query.Status.HasValue)
        {
            ticketsQuery = ticketsQuery.Where(t => t.Status == query.Status.Value);
        }

        if (query.Priority.HasValue)
        {
            ticketsQuery = ticketsQuery.Where(t => t.Priority == query.Priority.Value);
        }

        if (query.AssignedToUserId.HasValue)
        {
            ticketsQuery = ticketsQuery.Where(t => t.AssignedToUserId == query.AssignedToUserId.Value);
        }

        if (query.CategoryId.HasValue)
        {
            ticketsQuery = ticketsQuery.Where(t => t.CategoryId == query.CategoryId.Value);
        }

        int totalCount = await ticketsQuery.CountAsync(cancellationToken);

        var tickets = await ticketsQuery
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                t.Id,
                t.ProjectId,
                t.CreatedByUserId,
                t.AssignedToUserId,
                t.CategoryId,
                t.Title,
                t.Status,
                t.Priority,
                t.CreatedAt,
                t.UpdatedAt,
                t.FirstResponseDueAt,
                t.FirstResponseAt,
                t.ResolutionDueAt,
                t.ResolvedAt,
                t.ClosedAt
            })
            .ToListAsync(cancellationToken);

        var categoryIds = tickets
            .Where(t => t.CategoryId.HasValue)
            .Select(t => t.CategoryId!.Value)
            .Distinct()
            .ToList();

        Dictionary<Guid, TicketCategoryResponse> categoriesById = [];
        if (categoryIds.Count > 0)
        {
            categoriesById = await context.TicketCategories
                .AsNoTracking()
                .Where(tc => categoryIds.Contains(tc.Id))
                .ToDictionaryAsync(
                    tc => tc.Id,
                    tc => new TicketCategoryResponse(
                        tc.Id,
                        tc.ProjectId,
                        tc.Name,
                        tc.Description,
                        tc.BackgroundColor,
                        tc.ForegroundColor,
                        tc.Status),
                    cancellationToken);
        }

        var items = tickets.Select(t => new TicketSummaryResponse
        {
            Id = t.Id,
            ProjectId = t.ProjectId,
            CreatedByUserId = t.CreatedByUserId,
            AssignedToUserId = t.AssignedToUserId,
            Category = t.CategoryId.HasValue && categoriesById.TryGetValue(t.CategoryId.Value, out TicketCategoryResponse? category)
                ? category
                : null,
            Title = t.Title,
            Status = t.Status,
            Priority = t.Priority,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt,
            FirstResponseDueAt = t.FirstResponseDueAt,
            FirstResponseAt = t.FirstResponseAt,
            ResolutionDueAt = t.ResolutionDueAt,
            ResolvedAt = t.ResolvedAt,
            ClosedAt = t.ClosedAt
        }).ToList();

        return new PagedResponse<TicketSummaryResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
