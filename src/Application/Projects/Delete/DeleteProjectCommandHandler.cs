using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Projects;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Projects.Delete;

internal sealed class DeleteProjectCommandHandler(IApplicationDbContext context)
    : ICommandHandler<DeleteProjectCommand>
{
    public async Task<Result> Handle(DeleteProjectCommand command, CancellationToken cancellationToken)
    {
        Project? project = await context.Projects.SingleOrDefaultAsync(
            currentProject => currentProject.Id == command.ProjectId,
            cancellationToken);

        if (project is null)
        {
            return Result.Failure(ProjectErrors.NotFound(command.ProjectId));
        }

        project.Raise(new ProjectDeletedDomainEvent(project.Id));

        context.Projects.Remove(project);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
