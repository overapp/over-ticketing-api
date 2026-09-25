using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Application.Organizations;
using Domain.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Organizations.Get;

internal sealed class GetOrganizationsQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetOrganizationsQuery, PagedResponse<OrganizationResponse>>
{
    public async Task<Result<PagedResponse<OrganizationResponse>>> Handle(
        GetOrganizationsQuery query,
        CancellationToken cancellationToken)
    {
        int page = query.Page < 1 ? 1 : query.Page;
        int pageSize = query.PageSize < 1 ? 10 : query.PageSize;
        if (pageSize > 100)
        {
            pageSize = 100;
        }

        IQueryable<Organization> organizationsQuery = context.Organizations.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            string term = $"%{query.SearchTerm.Trim()}%";
            organizationsQuery = organizationsQuery.Where(organization => EF.Functions.Like(organization.Name, term));
        }

        if (query.Status.HasValue)
        {
            organizationsQuery = organizationsQuery.Where(organization => organization.Status == query.Status.Value);
        }

        int totalCount = await organizationsQuery.CountAsync(cancellationToken);

        List<OrganizationResponse> organizations = await organizationsQuery
            .OrderBy(organization => organization.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(organization => new OrganizationResponse
            {
                Id = organization.Id,
                Name = organization.Name,
                Logo = organization.Logo,
                Status = organization.Status
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<OrganizationResponse>
        {
            Items = organizations,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
