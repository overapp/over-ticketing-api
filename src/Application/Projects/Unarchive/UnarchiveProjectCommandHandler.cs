using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Projects;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Projects.Unarchive;

internal sealed class UnarchiveProjectCommandHandler(IApplicationDbContext context)
    : ICommandHandler<UnarchiveProjectCommand>
{
    public async Task<Result> Handle(UnarchiveProjectCommand command, CancellationToken cancellationToken)
    {
        Project? project = await context.Projects.SingleOrDefaultAsync(
            currentProject => currentProject.Id == command.ProjectId,
            cancellationToken);

        if (project is null)
        {
            return Result.Failure(ProjectErrors.NotFound(command.ProjectId));
        }

        if (project.Status == ProjectStatus.Active)
        {
            return Result.Failure(ProjectErrors.AlreadyActive(command.ProjectId));
        }

        project.Status = ProjectStatus.Active;
        project.Raise(new ProjectUnarchivedDomainEvent(project.Id));

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
