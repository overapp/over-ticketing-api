using Application.Abstractions.Messaging;
using Application.Common;
using Application.Projects;
using Domain.Projects;

namespace Application.Projects.Get;

public sealed record GetProjectsQuery(
    int Page = 1,
    int PageSize = 10,
    string? SearchTerm = null,
    Guid? OrganizationId = null,
    ProjectStatus? Status = null) : IQuery<PagedResponse<ProjectResponse>>;
