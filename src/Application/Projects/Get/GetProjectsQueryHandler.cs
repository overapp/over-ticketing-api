using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Application.Projects;
using Domain.Projects;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Projects.Get;

internal sealed class GetProjectsQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetProjectsQuery, PagedResponse<ProjectResponse>>
{
    public async Task<Result<PagedResponse<ProjectResponse>>> Handle(
        GetProjectsQuery query,
        CancellationToken cancellationToken)
    {
        int page = query.Page < 1 ? 1 : query.Page;
        int pageSize = query.PageSize < 1 ? 10 : query.PageSize;
        if (pageSize > 100)
        {
            pageSize = 100;
        }

        IQueryable<Project> projectsQuery = context.Projects.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            string term = $"%{query.SearchTerm.Trim()}%";
            projectsQuery = projectsQuery.Where(project =>
                EF.Functions.Like(project.Name, term) ||
                EF.Functions.Like(project.Description, term));
        }

        if (query.OrganizationId.HasValue)
        {
            projectsQuery = projectsQuery.Where(project => project.OrganizationId == query.OrganizationId.Value);
        }

        if (query.Status.HasValue)
        {
            projectsQuery = projectsQuery.Where(project => project.Status == query.Status.Value);
        }

        int totalCount = await projectsQuery.CountAsync(cancellationToken);

        List<ProjectResponse> projects = await projectsQuery
            .OrderBy(project => project.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(project => new ProjectResponse
            {
                Id = project.Id,
                OrganizationId = project.OrganizationId,
                Name = project.Name,
                Description = project.Description,
                Status = project.Status
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<ProjectResponse>
        {
            Items = projects,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
