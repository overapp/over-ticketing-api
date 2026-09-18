using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Projects;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Projects.Archive;

internal sealed class ArchiveProjectCommandHandler(IApplicationDbContext context)
    : ICommandHandler<ArchiveProjectCommand>
{
    public async Task<Result> Handle(ArchiveProjectCommand command, CancellationToken cancellationToken)
    {
        Project? project = await context.Projects.SingleOrDefaultAsync(
            currentProject => currentProject.Id == command.ProjectId,
            cancellationToken);

        if (project is null)
        {
            return Result.Failure(ProjectErrors.NotFound(command.ProjectId));
        }

        if (project.Status == ProjectStatus.Archived)
        {
            return Result.Failure(ProjectErrors.AlreadyArchived(command.ProjectId));
        }

        project.Status = ProjectStatus.Archived;
        project.Raise(new ProjectArchivedDomainEvent(project.Id));

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
