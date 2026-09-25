using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Projects;
using Domain.Projects;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Projects.GetById;

internal sealed class GetProjectByIdQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetProjectByIdQuery, ProjectResponse>
{
    public async Task<Result<ProjectResponse>> Handle(
        GetProjectByIdQuery query,
        CancellationToken cancellationToken)
    {
        ProjectResponse? project = await context.Projects
            .AsNoTracking()
            .Where(project => project.Id == query.ProjectId)
            .Select(project => new ProjectResponse
            {
                Id = project.Id,
                OrganizationId = project.OrganizationId,
                Name = project.Name,
                Description = project.Description,
                Status = project.Status
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (project is null)
        {
            return Result.Failure<ProjectResponse>(ProjectErrors.NotFound(query.ProjectId));
        }

        return project;
    }
}
