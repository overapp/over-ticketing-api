using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Organizations;
using Domain.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Organizations.GetById;

internal sealed class GetOrganizationByIdQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetOrganizationByIdQuery, OrganizationResponse>
{
    public async Task<Result<OrganizationResponse>> Handle(
        GetOrganizationByIdQuery query,
        CancellationToken cancellationToken)
    {
        OrganizationResponse? organization = await context.Organizations
            .AsNoTracking()
            .Where(organization => organization.Id == query.OrganizationId)
            .Select(organization => new OrganizationResponse
            {
                Id = organization.Id,
                Name = organization.Name,
                Logo = organization.Logo,
                Status = organization.Status
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (organization is null)
        {
            return Result.Failure<OrganizationResponse>(OrganizationErrors.NotFound(query.OrganizationId));
        }

        return organization;
    }
}
