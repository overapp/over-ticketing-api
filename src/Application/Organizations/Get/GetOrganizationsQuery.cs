using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Organizations;

namespace Application.Organizations.Get;

public sealed record GetOrganizationsQuery(
    int Page = 1,
    int PageSize = 10,
    string? SearchTerm = null,
    OrganizationStatus? Status = null) : IQuery<PagedResponse<OrganizationResponse>>;
